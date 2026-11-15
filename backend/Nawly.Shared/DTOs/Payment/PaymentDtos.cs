using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Shared.DTOs.Payment;

public sealed record InitiatePaymentResponseDto(
    Guid PaymentId,
    Guid OrderId,
    PaymentProvider Provider,
    string RedirectUrl,
    string? ClientSecret,
    DateTimeOffset ExpiresAt);

public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    PaymentMethod Method,
    PaymentProvider Provider,
    string? ProviderOrderId,
    string? ProviderTransactionId,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    string? FailureCode,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? CompletedAt,
    decimal TotalRefunded,
    decimal RemainingRefundable);

public sealed record RefundRequestDto(decimal Amount, string ReasonCode);

public sealed record RefundDto(
    Guid Id,
    Guid PaymentId,
    decimal Amount,
    string ReasonCode,
    RefundStatus Status,
    string? ProviderRefundId,
    Guid RequestedBy,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt);

public sealed record WebhookProcessResultDto(
    bool Accepted,
    bool AlreadyProcessed,
    string EventId,
    Guid? OrderId,
    PaymentStatus? ResultingStatus);
