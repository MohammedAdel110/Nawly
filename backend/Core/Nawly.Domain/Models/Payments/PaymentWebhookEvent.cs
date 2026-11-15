using Nawly.Domain.Models.Common;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Domain.Models.Payments;

public class PaymentWebhookEvent : BaseGuidEntity
{
    public PaymentProvider Provider { get; set; }
    public string ExternalEventId { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
}
