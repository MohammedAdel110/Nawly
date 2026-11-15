using Microsoft.Extensions.Logging;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Shared.DTOs.Delivery;
using Nawly.Shared.DTOs.Order;
using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Persistence.Adapters;

/// <summary>
/// Fallback realtime notifier when SignalR hub broadcaster is not yet registered in the host.
/// Logs events for diagnostics while SignalR hubs live in the Presentation/Web layer.
/// </summary>
public sealed class NullRealtimeNotifier(ILogger<NullRealtimeNotifier> logger) : IRealtimeNotifier
{
    public Task NotifyOrderStatusChangedAsync(
        Guid orderId,
        Guid customerId,
        Guid storeId,
        OrderStatus newStatus,
        DateTimeOffset? estimatedDeliveryAt,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[Realtime:NoOp] OrderStatusChanged OrderId={OrderId} CustomerId={CustomerId} StoreId={StoreId} Status={Status}",
            orderId, customerId, storeId, newStatus);
        return Task.CompletedTask;
    }

    public Task NotifyNewOrderAsync(Guid storeId, OrderSummaryDto order, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[Realtime:NoOp] NewOrder StoreId={StoreId} OrderId={OrderId} Number={OrderNumber}",
            storeId, order.Id, order.OrderNumber);
        return Task.CompletedTask;
    }

    public Task NotifyCourierAssignedAsync(
        Guid orderId,
        Guid courierId,
        string courierFirstName,
        string maskedPhone,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[Realtime:NoOp] CourierAssigned OrderId={OrderId} CourierId={CourierId} Name={CourierName}",
            orderId, courierId, courierFirstName);
        return Task.CompletedTask;
    }

    public Task NotifyCourierLocationAsync(
        Guid orderId,
        double latitude,
        double longitude,
        DateTimeOffset recordedAt,
        CancellationToken ct = default)
    {
        logger.LogDebug(
            "[Realtime:NoOp] CourierLocation OrderId={OrderId} Lat={Lat} Lng={Lng}",
            orderId, latitude, longitude);
        return Task.CompletedTask;
    }

    public Task NotifyDeliveryOfferAsync(Guid courierId, DeliveryOfferDto offer, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[Realtime:NoOp] DeliveryOffer CourierId={CourierId} OfferId={OfferId} Wave={Wave}",
            courierId, offer.OfferId, offer.Wave);
        return Task.CompletedTask;
    }

    public Task NotifyOfferExpiredOrTakenAsync(Guid courierId, Guid offerId, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[Realtime:NoOp] OfferClosed CourierId={CourierId} OfferId={OfferId}",
            courierId, offerId);
        return Task.CompletedTask;
    }

    public Task NotifyDispatchAlertAsync(Guid orderId, string message, CancellationToken ct = default)
    {
        logger.LogWarning(
            "[Realtime:NoOp] DispatchAlert OrderId={OrderId} Message={Message}",
            orderId, message);
        return Task.CompletedTask;
    }
}
