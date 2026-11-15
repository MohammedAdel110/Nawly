using Microsoft.AspNetCore.SignalR;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Shared.DTOs.Delivery;
using Nawly.Shared.DTOs.Order;
using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Web.Hubs;

public sealed class SignalRRealtimeNotifier(
    IHubContext<OrderTrackingHub> orderTrackingHub,
    IHubContext<DispatchHub> dispatchHub,
    IHubContext<StoreOrdersHub> storeOrdersHub) : IRealtimeNotifier
{
    public async Task NotifyOrderStatusChangedAsync(
        Guid orderId,
        Guid customerId,
        Guid storeId,
        OrderStatus status,
        DateTimeOffset? estimatedDeliveryAt,
        CancellationToken ct = default)
    {
        var payload = new
        {
            orderId,
            storeId,
            status = status.ToString(),
            estimatedDeliveryAt
        };

        await Task.WhenAll(
            orderTrackingHub.Clients.Groups([$"order:{orderId}", $"customer:{customerId}"])
                .SendAsync("OrderStatusChanged", payload, ct),
            storeOrdersHub.Clients.Group($"store:{storeId}")
                .SendAsync("OrderStatusChanged", payload, ct));
    }

    public Task NotifyNewOrderAsync(Guid storeId, OrderSummaryDto order, CancellationToken ct = default)
        => storeOrdersHub.Clients.Group($"store:{storeId}")
            .SendAsync("NewOrderReceived", order, ct);

    public Task NotifyCourierAssignedAsync(
        Guid orderId,
        Guid courierId,
        string courierFirstName,
        string maskedPhone,
        CancellationToken ct = default)
        => orderTrackingHub.Clients.Group($"order:{orderId}")
            .SendAsync("CourierAssigned", new
            {
                orderId,
                courierId,
                courierFirstName,
                maskedPhone
            }, ct);

    public Task NotifyCourierLocationAsync(
        Guid orderId,
        double latitude,
        double longitude,
        DateTimeOffset recordedAt,
        CancellationToken ct = default)
        => orderTrackingHub.Clients.Group($"order:{orderId}")
            .SendAsync("CourierLocationUpdated", new
            {
                orderId,
                latitude,
                longitude,
                recordedAt
            }, ct);

    public Task NotifyDeliveryOfferAsync(Guid courierId, DeliveryOfferDto offer, CancellationToken ct = default)
        => dispatchHub.Clients.Group($"courier:{courierId}")
            .SendAsync("DeliveryOfferReceived", offer, ct);

    public Task NotifyOfferExpiredOrTakenAsync(Guid courierId, Guid offerId, CancellationToken ct = default)
        => dispatchHub.Clients.Group($"courier:{courierId}")
            .SendAsync("DeliveryOfferClosed", new { offerId }, ct);

    public Task NotifyDispatchAlertAsync(Guid orderId, string reason, CancellationToken ct = default)
        => dispatchHub.Clients.Group("admin:dispatch")
            .SendAsync("DispatchAlert", new { orderId, reason }, ct);
}
