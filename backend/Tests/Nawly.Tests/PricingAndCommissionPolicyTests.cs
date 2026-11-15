using Nawly.Abstraction.IServices.Pricing;
using Nawly.Services.ServicesImplementation.Pricing.StrategyPattern;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Tests;

public sealed class PricingAndCommissionPolicyTests
{
    private readonly DefaultPricingPolicy _pricingPolicy = new();
    private readonly DefaultCommissionPolicy _commissionPolicy = new();

    [Fact]
    public void CalculateQuote_DeliveryWithZonePerKmAndCashRounding_ComputesServerAuthoritativeTotals()
    {
        var quote = _pricingPolicy.CalculateQuote(new PricingQuoteInput(
            Subtotal: 103.50m,
            ZoneBaseFee: 10m,
            ZonePerKmFee: 3m,
            DistanceKm: 2.5,
            FreeDeliveryThreshold: 250m,
            FulfilmentType: FulfilmentType.Delivery,
            PaymentMethod: PaymentMethod.Cash,
            DiscountAmount: 10m,
            IsCouponFreeDelivery: false,
            ServiceFeeRate: 0.05m,
            VatRate: 0m,
            CashRoundingIncrement: 5m));

        // Delivery fee = 10 + (3 * 2.5) = 17.50
        // Service fee = round(103.50 * 0.05) = 5.18
        // Raw total = 103.50 + 17.50 + 5.18 - 10.00 = 116.18
        // Nearest 5 EGP cash rounding -> 115.00 (CashRounding = -1.18)
        Assert.Equal(103.50m, quote.Subtotal);
        Assert.Equal(17.50m, quote.DeliveryFee);
        Assert.Equal(5.18m, quote.ServiceFee);
        Assert.Equal(10.00m, quote.DiscountAmount);
        Assert.Equal(115.00m, quote.Total);
        Assert.Equal(-1.18m, quote.CashRounding);
        Assert.Equal("EGP", quote.Currency);
    }

    [Fact]
    public void CalculateQuote_FreeDeliveryThresholdReached_ZeroesDeliveryFee()
    {
        var quote = _pricingPolicy.CalculateQuote(new PricingQuoteInput(
            Subtotal: 300m,
            ZoneBaseFee: 15m,
            ZonePerKmFee: 4m,
            DistanceKm: 3.0,
            FreeDeliveryThreshold: 250m,
            FulfilmentType: FulfilmentType.Delivery,
            PaymentMethod: PaymentMethod.Card,
            DiscountAmount: 0m,
            IsCouponFreeDelivery: false));

        Assert.Equal(0m, quote.DeliveryFee);
        Assert.Equal(300m, quote.Total);
        Assert.Equal(0m, quote.CashRounding);
    }

    [Fact]
    public void CalculateQuote_PickupOrder_AlwaysHasZeroDeliveryFee()
    {
        var quote = _pricingPolicy.CalculateQuote(new PricingQuoteInput(
            Subtotal: 80m,
            ZoneBaseFee: 15m,
            ZonePerKmFee: 5m,
            DistanceKm: 2.0,
            FreeDeliveryThreshold: null,
            FulfilmentType: FulfilmentType.Pickup,
            PaymentMethod: PaymentMethod.Cash,
            DiscountAmount: 0m,
            IsCouponFreeDelivery: false));

        Assert.Equal(0m, quote.DeliveryFee);
        Assert.Equal(80m, quote.Total);
    }

    [Fact]
    public void CalculateCommission_UsesStoreOverrideRateAndSubtractsStoreFundedDiscount()
    {
        var commissionWithOverride = _commissionPolicy.CalculateCommission(
            subtotal: 200m,
            storeFundedDiscount: 25m,
            storeOverrideRate: 0.10m,
            defaultRate: 0.12m);

        var commissionWithDefault = _commissionPolicy.CalculateCommission(
            subtotal: 200m,
            storeFundedDiscount: 0m,
            storeOverrideRate: null,
            defaultRate: 0.12m);

        Assert.Equal(17.50m, commissionWithOverride);
        Assert.Equal(24.00m, commissionWithDefault);
    }
}
