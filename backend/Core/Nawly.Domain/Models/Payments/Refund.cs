using Nawly.Domain.Models.Common;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Domain.Models.Payments;

public class Refund : BaseGuidEntity
{
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? ProviderRefundId { get; set; }
    public Guid RequestedBy { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public Payment Payment { get; set; } = null!;
}
