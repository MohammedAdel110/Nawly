using Nawly.Domain.Models.Ordering;
using Nawly.Shared.DTOs.Order;
using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Services.Mapping.OrderMapping;

public static class OrderMappingExtensions
{
    public static OrderItemOptionDto ToDto(this OrderItemOption option) => new(
        option.GroupName,
        option.GroupNameAr,
        option.OptionName,
        option.OptionNameAr,
        option.PriceDelta);

    public static OrderItemDto ToDto(this OrderItem item) => new(
        item.Id,
        item.ProductId,
        item.ProductName,
        item.ProductNameAr,
        item.UnitPrice,
        item.Quantity,
        item.LineTotal,
        item.Note,
        item.Options.Select(o => o.ToDto()).ToList());

    public static OrderStatusHistoryDto ToDto(this OrderStatusHistory history) => new(
        history.FromStatus,
        history.ToStatus,
        history.ActorRole,
        history.ReasonCode,
        history.ReasonText,
        history.At);

    public static OrderSummaryDto ToSummaryDto(this Order order) => new(
        order.Id,
        order.OrderNumber,
        order.StoreId,
        order.Store?.Name ?? string.Empty,
        order.Store?.NameAr ?? string.Empty,
        order.Status,
        order.PaymentMethod,
        order.PaymentStatus,
        order.FulfilmentType,
        order.Total,
        order.Currency,
        order.Items.Sum(i => i.Quantity),
        order.PlacedAt,
        order.EstimatedDeliveryAt);

    public static OrderDetailsDto ToDetailsDto(
        this Order order,
        OrderActorRole viewerRole = OrderActorRole.Customer,
        string? courierFirstName = null,
        string? courierPhone = null)
    {
        var isPickup = order.FulfilmentType == FulfilmentType.Pickup;
        var effectiveCourierPhone = viewerRole switch
        {
            OrderActorRole.Admin => courierPhone,
            OrderActorRole.Store => null,
            _ when order.IsTerminal => null,
            _ => courierPhone
        };

        var effectiveConfirmationCode = viewerRole == OrderActorRole.Customer && order.Status == OrderStatus.PickedUp
            ? order.DeliveryConfirmationCode
            : null;

        return new(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.StoreId,
            order.Store?.Name ?? string.Empty,
            order.Store?.NameAr ?? string.Empty,
            order.Status,
            order.PaymentMethod,
            order.PaymentStatus,
            order.FulfilmentType,
            order.Subtotal,
            order.DeliveryFee,
            order.ServiceFee,
            order.TaxAmount,
            order.DiscountAmount,
            order.CashRounding,
            order.Total,
            order.Currency,
            order.CustomerNote,
            order.LeaveAtDoor,
            order.CustomerName,
            order.CustomerPhone,
            isPickup ? null : order.AddressLabel,
            isPickup ? null : order.AreaName,
            isPickup ? null : order.AddressDescription,
            isPickup ? null : order.Building,
            isPickup ? null : order.Floor,
            isPickup ? null : order.Apartment,
            isPickup || viewerRole == OrderActorRole.Store ? null : order.DeliveryNotes,
            order.EstimatedReadyAt,
            order.EstimatedDeliveryAt,
            order.PlacedAt,
            order.AcceptedAt,
            order.DeliveredAt,
            order.CancelledAt,
            order.CancelReasonCode,
            order.CancelReasonText,
            courierFirstName,
            effectiveCourierPhone,
            effectiveConfirmationCode,
            order.Items.Select(i => i.ToDto()).ToList(),
            order.StatusHistory.OrderBy(h => h.At).Select(h => h.ToDto()).ToList());
    }
}
