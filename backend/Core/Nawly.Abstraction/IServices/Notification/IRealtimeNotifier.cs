using Nawly.Shared.DTOs.Delivery;
using Nawly.Shared.DTOs.Order;
using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Abstraction.IServices.Notification;

public interface IRealtimeNotifier
{
    Task NotifyOrderStatusChangedAsync(
        Guid orderId,
        Guid customerId,
        Guid storeId,
        OrderStatus status,
        DateTimeOffset? estimatedDeliveryAt,
        CancellationToken ct = default);

    Task NotifyNewOrderAsync(Guid storeId, OrderSummaryDto order, CancellationToken ct = default);

    Task NotifyCourierAssignedAsync(
        Guid orderId,
        Guid courierId,
        string courierFirstName,
        string maskedPhone,
        CancellationToken ct = default);

    Task NotifyCourierLocationAsync(
        Guid orderId,
        double latitude,
        double longitude,
        DateTimeOffset recordedAt,
        CancellationToken ct = default);

    Task NotifyDeliveryOfferAsync(Guid courierId, DeliveryOfferDto offer, CancellationToken ct = default);

    Task NotifyOfferExpiredOrTakenAsync(Guid courierId, Guid offerId, CancellationToken ct = default);

    Task NotifyDispatchAlertAsync(Guid orderId, string reason, CancellationToken ct = default);
}
