using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Abstraction.IServices.Payment;
using Nawly.Domain.Contracts.SpecificReposPattern;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Events;
using Nawly.Domain.Models.Finance;
using Nawly.Domain.Models.Payments;
using Nawly.Services.Mapping.OrderMapping;
using Nawly.Services.Mapping.PaymentMapping;
using Nawly.Services.Specifications.OrderSpec;
using Nawly.Services.Specifications.PaymentSpec;
using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Payment;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Payment;
using Nawly.Shared.EnumHelper.FinanceEnums;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Services.ServicesImplementation.Payment;

public sealed class PaymentService(
    IUnitOfWork unitOfWork,
    IOrderRepository orderRepository,
    IPaymentGateway paymentGateway,
    IDomainEventDispatcher domainEventDispatcher,
    IRealtimeNotifier realtimeNotifier,
    IConfiguration configuration,
    IClock clock) : IPaymentService
{
    public async Task<Result<InitiatePaymentResponseDto>> InitiateAsync(
        Guid userId,
        Guid orderId,
        Guid idempotencyKey,
        CancellationToken ct = default)
    {
        var onlineEnabled = await ServiceHelpers.GetBoolSettingAsync(unitOfWork, "Payments:OnlinePaymentsEnabled", false, ct);
        if (!onlineEnabled)
            return Result.Failure<InitiatePaymentResponseDto>(Error.Unprocessable("payment.online_disabled", "Online payments are currently disabled; only Cash is supported."));

        var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(orderId), ct);
        if (order is null)
            return Result.Failure<InitiatePaymentResponseDto>(Error.NotFound("payment.order_not_found", "Order not found."));

        var customer = await ServiceHelpers.ResolveCustomerProfileAsync(unitOfWork, userId, ct);
        if (customer is null || order.CustomerId != customer.Id)
            return Result.Failure<InitiatePaymentResponseDto>(Error.Forbidden("payment.forbidden", "You do not have access to pay for this order."));

        if (order.PaymentMethod == PaymentMethod.Cash)
            return Result.Failure<InitiatePaymentResponseDto>(Error.Validation("payment.cash_not_online", "Cash orders do not require online payment initiation."));

        if (order.Status != OrderStatus.PendingPayment)
            return Result.Failure<InitiatePaymentResponseDto>(Error.Conflict("payment.invalid_order_state", "Order is no longer awaiting payment."));

        var effectiveKey = idempotencyKey == Guid.Empty ? Guid.CreateVersion7() : idempotencyKey;
        var paymentRepo = unitOfWork.GetRepository<Domain.Models.Payments.Payment, Guid>();
        var existing = await paymentRepo.GetByIdWithSpecAsync(new PaymentByIdempotencyKeySpecification(order.Id, effectiveKey), ct);

        if (existing is not null && !string.IsNullOrWhiteSpace(existing.ProviderOrderId) && existing.Status == PaymentStatus.Pending)
        {
            var baseCallback = configuration["Payments:CallbackUrl"] ?? "https://nawly.local/api/v1/payments/webhook";
            var replayIntention = await paymentGateway.CreateIntentionAsync(
                new GatewayIntentionRequest(
                    existing.Id,
                    order.Id,
                    order.OrderNumber,
                    existing.Amount,
                    existing.Currency,
                    order.CustomerName,
                    order.CustomerPhone,
                    existing.Method,
                    baseCallback),
                ct);

            if (replayIntention.IsFailure)
                return Result.Failure<InitiatePaymentResponseDto>(replayIntention.Error);

            return Result.Success(new InitiatePaymentResponseDto(
                existing.Id,
                order.Id,
                existing.Provider,
                replayIntention.Value.RedirectUrl,
                replayIntention.Value.ClientSecret,
                replayIntention.Value.ExpiresAt));
        }

        var provider = order.PaymentMethod switch
        {
            PaymentMethod.Wallet => PaymentProvider.Paymob,
            PaymentMethod.Card => PaymentProvider.Paymob,
            _ => PaymentProvider.Fawry
        };

        var payment = new Domain.Models.Payments.Payment
        {
            OrderId = order.Id,
            Method = order.PaymentMethod,
            Provider = provider,
            Amount = order.Total,
            Currency = order.Currency,
            Status = PaymentStatus.Pending,
            IdempotencyKey = effectiveKey,
            InitiatedAt = clock.UtcNow
        };

        var callbackUrl = configuration["Payments:CallbackUrl"] ?? "https://nawly.local/api/v1/payments/webhook";
        var intentionResult = await paymentGateway.CreateIntentionAsync(
            new GatewayIntentionRequest(
                payment.Id,
                order.Id,
                order.OrderNumber,
                payment.Amount,
                payment.Currency,
                order.CustomerName,
                order.CustomerPhone,
                payment.Method,
                callbackUrl),
            ct);

        if (intentionResult.IsFailure)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureCode = intentionResult.Error.Code;
            await paymentRepo.AddAsync(payment, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Failure<InitiatePaymentResponseDto>(intentionResult.Error);
        }

        payment.ProviderOrderId = intentionResult.Value.ProviderOrderId;
        await paymentRepo.AddAsync(payment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new InitiatePaymentResponseDto(
            payment.Id,
            order.Id,
            payment.Provider,
            intentionResult.Value.RedirectUrl,
            intentionResult.Value.ClientSecret,
            intentionResult.Value.ExpiresAt));
    }

    public async Task<Result<WebhookProcessResultDto>> ProcessWebhookAsync(
        PaymentProvider provider,
        string rawPayload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct = default)
    {
        if (!paymentGateway.VerifyWebhookSignature(rawPayload, headers))
            return Result.Failure<WebhookProcessResultDto>(Error.Unauthorized("payment.invalid_webhook_signature", "Webhook signature verification failed."));

        string externalEventId;
        string? providerOrderId = null;
        string? providerTransactionId = null;
        string? failureCode = null;
        bool isSuccess = false;

        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            var root = doc.RootElement;

            externalEventId = TryGetString(root, "eventId")
                ?? TryGetString(root, "id")
                ?? ServiceHelpers.HashSecretCode(rawPayload);

            providerOrderId = TryGetString(root, "providerOrderId")
                ?? TryGetString(root, "orderId");

            providerTransactionId = TryGetString(root, "providerTransactionId")
                ?? TryGetString(root, "transactionId");

            failureCode = TryGetString(root, "failureCode");

            if (root.TryGetProperty("success", out var successProp) && successProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                isSuccess = successProp.GetBoolean();
            }
            else
            {
                var statusText = TryGetString(root, "status");
                isSuccess = string.Equals(statusText, "Captured", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(statusText, "Paid", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(statusText, "Succeeded", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (JsonException)
        {
            return Result.Failure<WebhookProcessResultDto>(Error.Validation("payment.invalid_webhook_payload", "Malformed JSON webhook payload."));
        }

        var webhookRepo = unitOfWork.GetRepository<PaymentWebhookEvent, Guid>();
        var existingEvent = await webhookRepo.GetByIdWithSpecAsync(new WebhookEventByExternalIdSpecification(provider, externalEventId), ct);
        if (existingEvent is { ProcessedAt: not null })
        {
            var matched = string.IsNullOrWhiteSpace(providerOrderId)
                ? null
                : await unitOfWork.GetRepository<Domain.Models.Payments.Payment, Guid>()
                    .GetByIdWithSpecAsync(new PaymentByProviderOrderIdSpecification(provider, providerOrderId), ct);

            return Result.Success(new WebhookProcessResultDto(
                Accepted: true,
                AlreadyProcessed: true,
                EventId: externalEventId,
                OrderId: matched?.OrderId,
                ResultingStatus: matched?.Status));
        }

        var signatureHeader = headers
            .FirstOrDefault(h => h.Key.Contains("signature", StringComparison.OrdinalIgnoreCase))
            .Value ?? string.Empty;

        var webhookEvent = existingEvent ?? new PaymentWebhookEvent
        {
            Provider = provider,
            ExternalEventId = externalEventId,
            PayloadHash = ServiceHelpers.HashSecretCode(rawPayload),
            Signature = signatureHeader,
            ReceivedAt = clock.UtcNow
        };

        Domain.Models.Payments.Payment? payment = null;
        if (!string.IsNullOrWhiteSpace(providerOrderId))
        {
            payment = await unitOfWork
                .GetRepository<Domain.Models.Payments.Payment, Guid>()
                .GetByIdWithSpecAsync(new PaymentByProviderOrderIdSpecification(provider, providerOrderId), ct);
        }

        Domain.Models.Ordering.Order? confirmedOrder = null;

        if (payment is not null)
        {
            payment.ProviderTransactionId = providerTransactionId ?? payment.ProviderTransactionId;

            if (isSuccess)
            {
                payment.Status = PaymentStatus.Captured;
                payment.CompletedAt = clock.UtcNow;

                var order = await orderRepository.GetByIdWithSpecAsync(new OrderWithDetailsSpecification(payment.OrderId), ct);
                if (order is { Status: OrderStatus.PendingPayment })
                {
                    var confirmResult = order.ConfirmOnlinePayment(clock.UtcNow);
                    if (confirmResult.IsSuccess)
                    {
                        orderRepository.Update(order);
                        confirmedOrder = order;
                    }
                }
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureCode = failureCode ?? "gateway_declined";
            }

            unitOfWork.GetRepository<Domain.Models.Payments.Payment, Guid>().Update(payment);
            webhookEvent.Result = payment.Status.ToString();
        }
        else
        {
            webhookEvent.Result = "PaymentNotMatched";
        }

        webhookEvent.ProcessedAt = clock.UtcNow;
        if (existingEvent is null)
            await webhookRepo.AddAsync(webhookEvent, ct);
        else
            webhookRepo.Update(webhookEvent);

        await unitOfWork.SaveChangesAsync(ct);

        if (confirmedOrder is not null)
        {
            await realtimeNotifier.NotifyNewOrderAsync(confirmedOrder.StoreId, confirmedOrder.ToSummaryDto(), ct);
            if (confirmedOrder.DomainEvents.Count > 0)
            {
                var events = confirmedOrder.DomainEvents.ToList();
                confirmedOrder.ClearDomainEvents();
                await domainEventDispatcher.DispatchAsync(events, ct);
            }
        }

        return Result.Success(new WebhookProcessResultDto(
            Accepted: true,
            AlreadyProcessed: false,
            EventId: externalEventId,
            OrderId: payment?.OrderId,
            ResultingStatus: payment?.Status));
    }

    public async Task<Result<PaginationResponse<PaymentDto>>> SearchPaymentsAsync(PaymentQueryParams queryParams, CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Domain.Models.Payments.Payment, Guid>();
        var items = await repo.GetAllWithSpecAsync(new PaymentsFilterSpecification(queryParams), ct);
        var total = await repo.GetCountAsync(new PaymentsCountSpecification(queryParams), ct);

        return Result.Success(new PaginationResponse<PaymentDto>(
            queryParams.PageIndex,
            queryParams.PageSize,
            total,
            items.Select(p => p.ToDto()).ToList()));
    }

    public async Task<Result<PaymentDto>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var payment = await unitOfWork
            .GetRepository<Domain.Models.Payments.Payment, Guid>()
            .GetByIdWithSpecAsync(new PaymentByOrderIdSpecification(orderId), ct);

        return payment is null
            ? Result.Failure<PaymentDto>(Error.NotFound("payment.not_found", "Payment not found for this order."))
            : Result.Success(payment.ToDto());
    }

    public async Task<Result<RefundDto>> RequestRefundAsync(
        Guid adminUserId,
        Guid paymentId,
        RefundRequestDto request,
        CancellationToken ct = default)
    {
        var paymentRepo = unitOfWork.GetRepository<Domain.Models.Payments.Payment, Guid>();
        var payment = await paymentRepo.GetByIdWithSpecAsync(new PaymentWithRefundsSpecification(paymentId), ct);
        if (payment is null)
            return Result.Failure<RefundDto>(Error.NotFound("payment.not_found", "Payment not found."));

        if (payment.Status is not (PaymentStatus.Captured or PaymentStatus.Paid or PaymentStatus.PartiallyRefunded))
            return Result.Failure<RefundDto>(Error.Conflict("payment.not_refundable", "Only captured or paid payments can be refunded."));

        if (request.Amount <= 0m || request.Amount > payment.RemainingRefundable)
            return Result.Failure<RefundDto>(Error.Validation("payment.invalid_refund_amount", $"Refund amount must be between 0.01 and {payment.RemainingRefundable:0.00} EGP."));

        if (string.IsNullOrWhiteSpace(payment.ProviderTransactionId))
            return Result.Failure<RefundDto>(Error.Unprocessable("payment.missing_provider_transaction", "Provider transaction ID is missing."));

        var gatewayResult = await paymentGateway.RefundAsync(
            payment.ProviderTransactionId,
            request.Amount,
            request.ReasonCode,
            ct);

        if (gatewayResult.IsFailure)
            return Result.Failure<RefundDto>(gatewayResult.Error);

        var refund = new Refund
        {
            PaymentId = payment.Id,
            Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            ReasonCode = request.ReasonCode.Trim(),
            Status = gatewayResult.Value.Status,
            ProviderRefundId = gatewayResult.Value.ProviderRefundId,
            RequestedBy = adminUserId,
            RequestedAt = clock.UtcNow,
            CompletedAt = gatewayResult.Value.Status == RefundStatus.Completed ? clock.UtcNow : null
        };

        await unitOfWork.GetRepository<Refund, Guid>().AddAsync(refund, ct);

        if (refund.Status == RefundStatus.Completed)
        {
            var isFullRefund = refund.Amount >= payment.RemainingRefundable;
            payment.Status = isFullRefund ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
            paymentRepo.Update(payment);

            await unitOfWork.GetRepository<LedgerEntry, Guid>().AddAsync(new LedgerEntry
            {
                AccountType = LedgerAccountType.Platform,
                AccountId = null,
                OrderId = payment.OrderId,
                PaymentId = payment.Id,
                Kind = LedgerEntryKind.Refund,
                Direction = LedgerDirection.Debit,
                Amount = refund.Amount,
                Description = $"Refund processed for payment {payment.Id} ({refund.ReasonCode})",
                CreatedAt = clock.UtcNow
            }, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(refund.ToDto());
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var prop))
            return null;

        return prop.ValueKind switch
        {
            JsonValueKind.String => prop.GetString(),
            JsonValueKind.Number => prop.GetRawText(),
            _ => null
        };
    }
}
