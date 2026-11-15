using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Abstraction.IServices.Pricing;

public sealed record PricingQuoteInput(
    decimal Subtotal,
    decimal ZoneBaseFee,
    decimal? ZonePerKmFee,
    double? DistanceKm,
    decimal? FreeDeliveryThreshold,
    FulfilmentType FulfilmentType,
    PaymentMethod PaymentMethod,
    decimal DiscountAmount,
    bool IsCouponFreeDelivery,
    decimal ServiceFeeRate = 0m,
    decimal VatRate = 0m,
    decimal CashRoundingIncrement = 0m);

public sealed record PricingQuoteResult(
    decimal Subtotal,
    decimal DeliveryFee,
    decimal ServiceFee,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal CashRounding,
    decimal Total,
    string Currency = "EGP");

public interface IPricingPolicy
{
    PricingQuoteResult CalculateQuote(PricingQuoteInput input);
}
