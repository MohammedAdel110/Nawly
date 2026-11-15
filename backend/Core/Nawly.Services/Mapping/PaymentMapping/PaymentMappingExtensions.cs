using Nawly.Domain.Models.Payments;
using Nawly.Shared.DTOs.Payment;

namespace Nawly.Services.Mapping.PaymentMapping;

public static class PaymentMappingExtensions
{
    public static RefundDto ToDto(this Refund refund) => new(
        refund.Id,
        refund.PaymentId,
        refund.Amount,
        refund.ReasonCode,
        refund.Status,
        refund.ProviderRefundId,
        refund.RequestedBy,
        refund.RequestedAt,
        refund.CompletedAt);

    public static PaymentDto ToDto(this Payment payment) => new(
        payment.Id,
        payment.OrderId,
        payment.Method,
        payment.Provider,
        payment.ProviderOrderId,
        payment.ProviderTransactionId,
        payment.Amount,
        payment.Currency,
        payment.Status,
        payment.FailureCode,
        payment.InitiatedAt,
        payment.CompletedAt,
        payment.TotalRefunded,
        payment.RemainingRefundable);
}
