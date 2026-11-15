using Nawly.Abstraction.IServices.Pricing;

namespace Nawly.Services.ServicesImplementation.Pricing.StrategyPattern;

/// <summary>
/// Computes platform commission on order subtotal net of store-funded discounts (SRS §5.7).
/// </summary>
public sealed class DefaultCommissionPolicy : ICommissionPolicy
{
    public decimal CalculateCommission(
        decimal subtotal,
        decimal storeFundedDiscount,
        decimal? storeOverrideRate,
        decimal defaultRate)
    {
        var effectiveRate = Math.Clamp(storeOverrideRate ?? defaultRate, 0m, 1m);
        var commissionableBase = Math.Max(0m, subtotal - Math.Max(0m, storeFundedDiscount));
        return decimal.Round(commissionableBase * effectiveRate, 2, MidpointRounding.AwayFromZero);
    }
}
