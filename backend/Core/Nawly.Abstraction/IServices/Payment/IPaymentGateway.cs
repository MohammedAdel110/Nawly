using Nawly.Shared.Common.Results;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Abstraction.IServices.Payment;

public sealed record GatewayIntentionRequest(
    Guid PaymentId,
    Guid OrderId,
    string OrderNumber,
    decimal Amount,
    string Currency,
    string CustomerName,
    string CustomerPhone,
    PaymentMethod Method,
    string CallbackUrl);

public sealed record GatewayIntentionResult(
    string ProviderOrderId,
    string RedirectUrl,
    string? ClientSecret,
    DateTimeOffset ExpiresAt);

public sealed record GatewayTransactionStatus(
    string ProviderTransactionId,
    string ProviderOrderId,
    PaymentStatus Status,
    decimal Amount,
    string? FailureCode = null);

public sealed record GatewayRefundResult(
    string ProviderRefundId,
    RefundStatus Status);

public interface IPaymentGateway
{
    Task<Result<GatewayIntentionResult>> CreateIntentionAsync(
        GatewayIntentionRequest request,
        CancellationToken ct = default);

    bool VerifyWebhookSignature(string rawPayload, IReadOnlyDictionary<string, string> headers);

    Task<Result<GatewayTransactionStatus>> GetTransactionStatusAsync(
        string providerOrderId,
        CancellationToken ct = default);

    Task<Result<GatewayRefundResult>> RefundAsync(
        string providerTransactionId,
        decimal amount,
        string reason,
        CancellationToken ct = default);
}
