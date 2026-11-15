using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Domain.Events;

public sealed record DeliveryOfferedEvent(Guid DeliveryId, Guid OrderId, Guid CourierId, int Wave, DateTimeOffset ExpiresAt) : DomainEventBase;
public sealed record DeliveryAssignedEvent(Guid DeliveryId, Guid OrderId, Guid CourierId) : DomainEventBase;
public sealed record DeliveryStatusChangedEvent(Guid DeliveryId, Guid OrderId, Guid? CourierId, DeliveryStatus FromStatus, DeliveryStatus ToStatus) : DomainEventBase;
public sealed record DeliveryFailedEvent(Guid DeliveryId, Guid OrderId, Guid? CourierId, DeliveryFailureReason ReasonCode, string? FailureNote) : DomainEventBase;
public sealed record DispatchEscalatedEvent(Guid DeliveryId, Guid OrderId, int AttemptedWaves) : DomainEventBase;
