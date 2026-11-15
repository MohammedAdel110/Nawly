using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Promotions;
using Nawly.Shared.DTOs.Promotion;

namespace Nawly.Services.Mapping.PromotionMapping;

public static class PromotionMappingExtensions
{
    public static CouponDto ToDto(this Coupon coupon) => new(
        coupon.Id,
        coupon.Code,
        coupon.Type,
        coupon.Value,
        coupon.MaxDiscount,
        coupon.MinOrderAmount,
        coupon.StartsAt,
        coupon.EndsAt,
        coupon.UsageLimitTotal,
        coupon.UsageLimitPerUser,
        coupon.StoreId,
        coupon.Store?.Name,
        coupon.FundedBy,
        coupon.IsActive);

    public static BannerDto ToDto(this Banner banner) => new(
        banner.Id,
        banner.Title,
        banner.TitleAr,
        banner.ImageRef,
        banner.TargetType,
        banner.TargetId,
        banner.TargetUrl,
        banner.StartsAt,
        banner.EndsAt,
        banner.SortOrder,
        banner.IsActive);
}
