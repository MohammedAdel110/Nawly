using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Common;
using Nawly.Shared.Common.Results;
using Nawly.Shared.EnumHelper.PromotionEnums;

namespace Nawly.Domain.Models.Promotions;

public class Coupon : BaseGuidEntity
{
    public string Code { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int? UsageLimitTotal { get; set; }
    public int? UsageLimitPerUser { get; set; }
    public Guid? StoreId { get; set; }
    public CouponFundingSource FundedBy { get; set; } = CouponFundingSource.Platform;
    public bool IsActive { get; set; } = true;

    public Store? Store { get; set; }
    public ICollection<CouponRedemption> Redemptions { get; set; } = [];

    public Result<decimal> CalculateDiscount(
        decimal subtotal,
        decimal deliveryFee,
        Guid storeId,
        int totalRedemptions,
        int userRedemptions,
        DateTimeOffset utcNow)
    {
        if (!IsActive || IsDeleted)
            return Result.Failure<decimal>(Error.Unprocessable("coupon.inactive", "Coupon is inactive."));

        if (utcNow < StartsAt || utcNow > EndsAt)
            return Result.Failure<decimal>(Error.Unprocessable("coupon.expired", "Coupon is outside its validity window."));

        if (StoreId.HasValue && StoreId.Value != storeId)
            return Result.Failure<decimal>(Error.Unprocessable("coupon.store_mismatch", "Coupon does not apply to this store."));

        if (MinOrderAmount.HasValue && subtotal < MinOrderAmount.Value)
            return Result.Failure<decimal>(Error.Unprocessable("coupon.min_order_not_met", $"Minimum order for this coupon is {MinOrderAmount.Value:0.00} EGP."));

        if (UsageLimitTotal.HasValue && totalRedemptions >= UsageLimitTotal.Value)
            return Result.Failure<decimal>(Error.Unprocessable("coupon.usage_limit_reached", "Coupon usage limit has been reached."));

        if (UsageLimitPerUser.HasValue && userRedemptions >= UsageLimitPerUser.Value)
            return Result.Failure<decimal>(Error.Unprocessable("coupon.user_limit_reached", "You have already used this coupon."));

        var rawDiscount = Type switch
        {
            CouponType.Percent => subtotal * (Value / 100m),
            CouponType.Fixed => Value,
            CouponType.FreeDelivery => deliveryFee,
            _ => 0m
        };

        if (MaxDiscount.HasValue)
            rawDiscount = Math.Min(rawDiscount, MaxDiscount.Value);

        var maxApplicable = Type == CouponType.FreeDelivery ? deliveryFee : subtotal;
        var finalDiscount = decimal.Round(Math.Min(rawDiscount, maxApplicable), 2, MidpointRounding.AwayFromZero);
        return Result.Success(Math.Max(0m, finalDiscount));
    }
}
