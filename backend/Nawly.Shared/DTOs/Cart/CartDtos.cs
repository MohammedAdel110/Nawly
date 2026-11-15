using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Shared.DTOs.Cart;

public sealed record CartItemOptionDto(
    Guid ProductOptionId,
    string GroupName,
    string GroupNameAr,
    string OptionName,
    string OptionNameAr,
    decimal PriceDelta);

public sealed record CartItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductNameAr,
    string? ImageRef,
    decimal BaseUnitPrice,
    int Quantity,
    decimal LineTotal,
    string? Note,
    bool IsAvailable,
    IReadOnlyList<CartItemOptionDto> SelectedOptions);

public sealed record CartDto(
    Guid Id,
    Guid StoreId,
    string StoreName,
    string StoreNameAr,
    decimal ItemsSubtotal,
    IReadOnlyList<CartItemDto> Items);

public sealed record UpsertCartItemDto(
    Guid ProductId,
    int Quantity,
    string? Note,
    IReadOnlyList<Guid> SelectedOptionIds,
    bool ReplaceCartIfDifferentStore = false);

public sealed record CartQuoteRequestDto(
    Guid? AddressId,
    FulfilmentType FulfilmentType = FulfilmentType.Delivery,
    PaymentMethod PaymentMethod = PaymentMethod.Cash,
    string? CouponCode = null);

public sealed record QuoteViolationDto(string Code, string Message, Guid? ProductId = null);

public sealed record CartQuoteResponseDto(
    bool IsValid,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal ServiceFee,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal CashRounding,
    decimal Total,
    string Currency,
    int EstimatedMinutes,
    bool CouponApplied,
    string? AppliedCouponCode,
    IReadOnlyList<QuoteViolationDto> Violations);
