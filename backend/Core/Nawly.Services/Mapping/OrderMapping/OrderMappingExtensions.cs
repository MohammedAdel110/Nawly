using Nawly.Domain.Models.Ordering;
using Nawly.Shared.DTOs.Order;

namespace Nawly.Services.Mapping.OrderMapping;

public static class OrderMappingExtensions
{
    public static OrderItemOptionDto ToDto(this OrderItemOption option) => new(
        option.GroupName,
        option.OptionName,
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
        string? courierFirstName = null,
        string? courierPhone = null) => new(
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
            order.AddressLabel,
            order.AreaName,
            order.AddressDescription,
            order.Building,
            order.Floor,
            order.Apartment,
            order.DeliveryNotes,
            order.EstimatedReadyAt,
            order.EstimatedDeliveryAt,
            order.PlacedAt,
            order.AcceptedAt,
            order.DeliveredAt,
            order.CancelledAt,
            order.CancelReasonCode,
            order.CancelReasonText,
            courierFirstName,
            courierPhone,
            order.Items.Select(i => i.ToDto()).ToList(),
            order.StatusHistory.OrderBy(h => h.At).Select(h => h.ToDto()).ToList());
}
