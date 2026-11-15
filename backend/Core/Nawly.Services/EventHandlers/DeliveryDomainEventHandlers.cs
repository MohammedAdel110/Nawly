using Nawly.Abstraction.IServices.Notification;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Events;
using Nawly.Domain.ValueObjects;
using Nawly.Services.Specifications.DeliverySpec;

namespace Nawly.Services.EventHandlers;

public sealed class DeliveryAssignedEventHandler(
    IUnitOfWork unitOfWork,
    IRealtimeNotifier realtimeNotifier) : IDomainEventHandler<DeliveryAssignedEvent>
{
    public async Task HandleAsync(DeliveryAssignedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var delivery = await unitOfWork
            .GetRepository<Domain.Models.Delivery.Delivery, Guid>()
            .GetByIdWithSpecAsync(new DeliveryWithDetailsSpecification(domainEvent.DeliveryId, asNoTracking: true), cancellationToken);

        if (delivery?.Courier?.User is null)
            return;

        var firstName = delivery.Courier.User.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Courier";
        var maskedPhone = PhoneNumber.MaskRaw(delivery.Courier.User.PhoneNumber);

        await realtimeNotifier.NotifyCourierAssignedAsync(
            domainEvent.OrderId,
            domainEvent.CourierId,
            firstName,
            maskedPhone,
            cancellationToken);
    }
}

public sealed class DispatchEscalatedEventHandler(
    IRealtimeNotifier realtimeNotifier) : IDomainEventHandler<DispatchEscalatedEvent>
{
    public Task HandleAsync(DispatchEscalatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        return realtimeNotifier.NotifyDispatchAlertAsync(
            domainEvent.OrderId,
            $"No courier accepted after {domainEvent.AttemptedWaves} dispatch wave(s).",
            cancellationToken);
    }
}
