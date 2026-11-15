using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Ordering;
using Nawly.Domain.ValueObjects;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Domain.Models.Payments;

public class Payment : BaseGuidEntity
{
    public Guid OrderId { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentProvider Provider { get; set; }
    public string? ProviderOrderId { get; set; }
    public string? ProviderTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = Money.DefaultCurrency;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? FailureCode { get; set; }
    public Guid IdempotencyKey { get; set; }
    public DateTimeOffset InitiatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public uint RowVersion { get; set; }

    public Order Order { get; set; } = null!;
    public ICollection<Refund> Refunds { get; set; } = [];

    public decimal TotalRefunded => Refunds
        .Where(r => r.Status == RefundStatus.Completed)
        .Sum(r => r.Amount);

    public decimal RemainingRefundable => Math.Max(0m, Amount - TotalRefunded);
}
