using Nawly.Abstraction.IServices.Finance;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Events;
using Nawly.Domain.Models.Identity;
using Nawly.Shared.EnumHelper.NotificationEnums;
using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Services.EventHandlers;

internal static class OrderNotificationHelper
{
    public static async Task NotifyCustomerAsync(
        IUnitOfWork unitOfWork,
        IRealtimeNotifier realtimeNotifier,
        INotificationService notificationService,
        Guid orderId,
        Guid customerProfileId,
        Guid storeId,
        OrderStatus status,
        NotificationType notificationType,
        string title,
        string body,
        DateTimeOffset? estimatedDeliveryAt,
        CancellationToken ct)
    {
        var customer = await unitOfWork.GetRepository<CustomerProfile, Guid>().GetByIdAsync(customerProfileId, ct);
        var customerUserId = customer?.UserId ?? customerProfileId;

        await realtimeNotifier.NotifyOrderStatusChangedAsync(
            orderId,
            customerUserId,
            storeId,
            status,
            estimatedDeliveryAt,
            ct);

        await notificationService.SendToUserAsync(
            customerUserId,
            notificationType,
            title,
            body,
            NotificationChannelFlags.InApp,
            null,
            ct);
    }
}

public sealed class OrderDeliveredEventHandler(
    IUnitOfWork unitOfWork,
    IFinanceService financeService,
    IRealtimeNotifier realtimeNotifier,
    INotificationService notificationService) : IDomainEventHandler<OrderDeliveredEvent>
{
    public async Task HandleAsync(OrderDeliveredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await financeService.RecordOrderCompletedLedgerAsync(domainEvent.OrderId, cancellationToken);

        await OrderNotificationHelper.NotifyCustomerAsync(
            unitOfWork,
            realtimeNotifier,
            notificationService,
            domainEvent.OrderId,
            domainEvent.CustomerId,
            domainEvent.StoreId,
            OrderStatus.Delivered,
            NotificationType.OrderDelivered,
            "Order Delivered",
            "Your order has been delivered. Enjoy!",
            null,
            cancellationToken);
    }
}

public sealed class OrderAcceptedEventHandler(
    IUnitOfWork unitOfWork,
    IRealtimeNotifier realtimeNotifier,
    INotificationService notificationService) : IDomainEventHandler<OrderAcceptedEvent>
{
    public Task HandleAsync(OrderAcceptedEvent domainEvent, CancellationToken cancellationToken = default)
        => OrderNotificationHelper.NotifyCustomerAsync(
            unitOfWork,
            realtimeNotifier,
            notificationService,
            domainEvent.OrderId,
            domainEvent.CustomerId,
            domainEvent.StoreId,
            OrderStatus.Accepted,
            NotificationType.OrderAccepted,
            "Order Accepted",
            $"The store accepted your order (prep ~{domainEvent.PrepMinutes} min).",
            domainEvent.OccurredAt.AddMinutes(domainEvent.PrepMinutes + 20),
            cancellationToken);
}

public sealed class OrderPreparingEventHandler(
    IUnitOfWork unitOfWork,
    IRealtimeNotifier realtimeNotifier,
    INotificationService notificationService) : IDomainEventHandler<OrderPreparingEvent>
{
    public Task HandleAsync(OrderPreparingEvent domainEvent, CancellationToken cancellationToken = default)
        => OrderNotificationHelper.NotifyCustomerAsync(
            unitOfWork,
            realtimeNotifier,
            notificationService,
            domainEvent.OrderId,
            domainEvent.CustomerId,
            domainEvent.StoreId,
            OrderStatus.Preparing,
            NotificationType.OrderPreparing,
            "Order Preparing",
            "Your order is now being prepared.",
            null,
            cancellationToken);
}

public sealed class OrderReadyForPickupEventHandler(
    IUnitOfWork unitOfWork,
    IRealtimeNotifier realtimeNotifier,
    INotificationService notificationService) : IDomainEventHandler<OrderReadyForPickupEvent>
{
    public Task HandleAsync(OrderReadyForPickupEvent domainEvent, CancellationToken cancellationToken = default)
        => OrderNotificationHelper.NotifyCustomerAsync(
            unitOfWork,
            realtimeNotifier,
            notificationService,
            domainEvent.OrderId,
            domainEvent.CustomerId,
            domainEvent.StoreId,
            OrderStatus.ReadyForPickup,
            NotificationType.OrderReady,
            "Order Ready",
            domainEvent.FulfilmentType == FulfilmentType.Pickup
                ? "Your order is ready for pickup at the store."
                : "Your order is ready and awaiting courier pickup.",
            null,
            cancellationToken);
}

public sealed class OrderPickedUpEventHandler(
    IUnitOfWork unitOfWork,
    IRealtimeNotifier realtimeNotifier,
    INotificationService notificationService) : IDomainEventHandler<OrderPickedUpEvent>
{
    public Task HandleAsync(OrderPickedUpEvent domainEvent, CancellationToken cancellationToken = default)
        => OrderNotificationHelper.NotifyCustomerAsync(
            unitOfWork,
            realtimeNotifier,
            notificationService,
            domainEvent.OrderId,
            domainEvent.CustomerId,
            domainEvent.StoreId,
            OrderStatus.PickedUp,
            NotificationType.OrderPickedUp,
            "Order Picked Up",
            "Your courier is on the way with your order.",
            null,
            cancellationToken);
}

public sealed class OrderRejectedEventHandler(
    IUnitOfWork unitOfWork,
    IRealtimeNotifier realtimeNotifier,
    INotificationService notificationService) : IDomainEventHandler<OrderRejectedEvent>
{
    public Task HandleAsync(OrderRejectedEvent domainEvent, CancellationToken cancellationToken = default)
        => OrderNotificationHelper.NotifyCustomerAsync(
            unitOfWork,
            realtimeNotifier,
            notificationService,
            domainEvent.OrderId,
            domainEvent.CustomerId,
            domainEvent.StoreId,
            OrderStatus.Rejected,
            NotificationType.OrderRejected,
            "Order Rejected",
            domainEvent.ReasonText ?? "Sorry, the store could not fulfill your order.",
            null,
            cancellationToken);
}

public sealed class OrderCancelledEventHandler(
    IUnitOfWork unitOfWork,
    IRealtimeNotifier realtimeNotifier,
    INotificationService notificationService) : IDomainEventHandler<OrderCancelledEvent>
{
    public Task HandleAsync(OrderCancelledEvent domainEvent, CancellationToken cancellationToken = default)
        => OrderNotificationHelper.NotifyCustomerAsync(
            unitOfWork,
            realtimeNotifier,
            notificationService,
            domainEvent.OrderId,
            domainEvent.CustomerId,
            domainEvent.StoreId,
            OrderStatus.Cancelled,
            NotificationType.OrderCancelled,
            "Order Cancelled",
            "Your order has been cancelled.",
            null,
            cancellationToken);
}
