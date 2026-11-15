using Nawly.Abstraction.IServices.Pricing;
using Nawly.Domain.ValueObjects;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Services.ServicesImplementation.Pricing.StrategyPattern;

/// <summary>
/// Server-authoritative checkout pricing calculator enforcing EGP 2-decimal rounding
/// and optional COD cash-increment rounding (SRS §5.3.2, §5.4).
/// </summary>
public sealed class DefaultPricingPolicy : IPricingPolicy
{
    public PricingQuoteResult CalculateQuote(PricingQuoteInput input)
    {
        var subtotal = RoundMoney(Math.Max(0m, input.Subtotal));

        var deliveryFee = CalculateDeliveryFee(input, subtotal);
        var serviceFee = RoundMoney(subtotal * Math.Max(0m, input.ServiceFeeRate));

        var discount = input.IsCouponFreeDelivery && input.DiscountAmount <= 0m
            ? deliveryFee
            : RoundMoney(Math.Max(0m, input.DiscountAmount));

        var maxDiscountable = input.IsCouponFreeDelivery
            ? deliveryFee
            : subtotal + deliveryFee;

        discount = Math.Min(discount, maxDiscountable);

        var taxableBase = input.IsCouponFreeDelivery
            ? subtotal
            : Math.Max(0m, subtotal - discount);

        var taxAmount = RoundMoney(taxableBase * Math.Max(0m, input.VatRate));

        var rawTotal = Math.Max(0m, subtotal + deliveryFee + serviceFee + taxAmount - discount);
        var cashRounding = 0m;
        var finalTotal = RoundMoney(rawTotal);

        if (input.PaymentMethod == PaymentMethod.Cash && input.CashRoundingIncrement > 0m)
        {
            var roundedCash = new Money(rawTotal).RoundCashTo(input.CashRoundingIncrement).Amount;
            cashRounding = RoundMoney(roundedCash - finalTotal);
            finalTotal = roundedCash;
        }

        return new PricingQuoteResult(
            subtotal,
            deliveryFee,
            serviceFee,
            taxAmount,
            discount,
            cashRounding,
            finalTotal,
            Money.DefaultCurrency);
    }

    private static decimal CalculateDeliveryFee(PricingQuoteInput input, decimal subtotal)
    {
        if (input.FulfilmentType == FulfilmentType.Pickup)
            return 0m;

        var fee = Math.Max(0m, input.ZoneBaseFee);
        if (input.ZonePerKmFee.HasValue && input.DistanceKm.HasValue && input.DistanceKm.Value > 0)
        {
            fee += input.ZonePerKmFee.Value * (decimal)input.DistanceKm.Value;
        }

        if (!input.IsCouponFreeDelivery && input.FreeDeliveryThreshold.HasValue && subtotal >= input.FreeDeliveryThreshold.Value)
            return 0m;

        return RoundMoney(fee);
    }

    private static decimal RoundMoney(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
