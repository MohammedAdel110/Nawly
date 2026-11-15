namespace Nawly.Abstraction.IServices.Pricing;

public interface ICommissionPolicy
{
    decimal CalculateCommission(
        decimal subtotal,
        decimal storeFundedDiscount,
        decimal? storeOverrideRate,
        decimal defaultRate);
}
