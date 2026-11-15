using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Shared.DTOs.Order;

public sealed record PlaceOrderRequestDto(
    Guid? AddressId,
    FulfilmentType FulfilmentType = FulfilmentType.Delivery,
    PaymentMethod PaymentMethod = PaymentMethod.Cash,
    string? CouponCode = null,
    string? CustomerNote = null,
    bool LeaveAtDoor = false,
    Guid? IdempotencyKey = null);

public sealed record AcceptOrderDto(int PrepMinutes);
public sealed record RejectOrderDto(CancelReasonCode ReasonCode, string? ReasonText);
public sealed record CancelOrderDto(CancelReasonCode ReasonCode, string? ReasonText);
public sealed record AdminChangeOrderStatusDto(OrderStatus NewStatus, CancelReasonCode? ReasonCode, string ReasonText);

public sealed record OrderItemOptionDto(
    string GroupName,
    string GroupNameAr,
    string OptionName,
    string OptionNameAr,
    decimal PriceDelta);

public sealed record OrderItemDto(
    Guid Id,
    Guid? ProductId,
    string ProductName,
    string ProductNameAr,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? Note,
    IReadOnlyList<OrderItemOptionDto> Options);

public sealed record OrderStatusHistoryDto(
    OrderStatus FromStatus,
    OrderStatus ToStatus,
    OrderActorRole ActorRole,
    CancelReasonCode? ReasonCode,
    string? ReasonText,
    DateTimeOffset At);

public sealed record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    Guid StoreId,
    string StoreName,
    string StoreNameAr,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    FulfilmentType FulfilmentType,
    decimal Total,
    string Currency,
    int ItemCount,
    DateTimeOffset PlacedAt,
    DateTimeOffset? EstimatedDeliveryAt);

public sealed record OrderDetailsDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    Guid StoreId,
    string StoreName,
    string StoreNameAr,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    FulfilmentType FulfilmentType,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal ServiceFee,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal CashRounding,
    decimal Total,
    string Currency,
    string? CustomerNote,
    bool LeaveAtDoor,
    string CustomerName,
    string CustomerPhone,
    string? AddressLabel,
    string? AreaName,
    string? AddressDescription,
    string? Building,
    string? Floor,
    string? Apartment,
    string? DeliveryNotes,
    DateTimeOffset? EstimatedReadyAt,
    DateTimeOffset? EstimatedDeliveryAt,
    DateTimeOffset PlacedAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? CancelledAt,
    CancelReasonCode? CancelReasonCode,
    string? CancelReasonText,
    string? CourierFirstName,
    string? CourierPhone,
    string? DeliveryConfirmationCode,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderStatusHistoryDto> History);
