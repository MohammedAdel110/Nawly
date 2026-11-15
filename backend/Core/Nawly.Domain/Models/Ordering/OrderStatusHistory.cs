using Nawly.Domain.Models.Common;
using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Domain.Models.Ordering;

public class OrderStatusHistory : BaseGuidEntity
{
    public Guid OrderId { get; set; }
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public Guid? ActorUserId { get; set; }
    public OrderActorRole ActorRole { get; set; }
    public CancelReasonCode? ReasonCode { get; set; }
    public string? ReasonText { get; set; }
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}
