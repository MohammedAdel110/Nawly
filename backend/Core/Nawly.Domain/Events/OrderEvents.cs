using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Domain.Events;

public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public sealed record OrderPlacedEvent(Guid OrderId, Guid StoreId, Guid CustomerId, PaymentMethod PaymentMethod) : DomainEventBase;
public sealed record OrderAcceptedEvent(Guid OrderId, Guid StoreId, Guid CustomerId, int PrepMinutes) : DomainEventBase;
public sealed record OrderRejectedEvent(Guid OrderId, Guid StoreId, Guid CustomerId, CancelReasonCode ReasonCode, string? ReasonText) : DomainEventBase;
public sealed record OrderPreparingEvent(Guid OrderId, Guid StoreId, Guid CustomerId) : DomainEventBase;
public sealed record OrderReadyForPickupEvent(Guid OrderId, Guid StoreId, Guid CustomerId, FulfilmentType FulfilmentType) : DomainEventBase;
public sealed record OrderPickedUpEvent(Guid OrderId, Guid StoreId, Guid CustomerId, Guid? CourierId) : DomainEventBase;
public sealed record OrderDeliveredEvent(Guid OrderId, Guid StoreId, Guid CustomerId, Guid? CourierId, PaymentMethod PaymentMethod, decimal Total) : DomainEventBase;
public sealed record OrderCancelledEvent(Guid OrderId, Guid StoreId, Guid CustomerId, OrderActorRole CancelledBy, CancelReasonCode ReasonCode, OrderStatus PreviousStatus) : DomainEventBase;
public sealed record OrderFailedEvent(Guid OrderId, Guid StoreId, Guid CustomerId, string? Reason) : DomainEventBase;
