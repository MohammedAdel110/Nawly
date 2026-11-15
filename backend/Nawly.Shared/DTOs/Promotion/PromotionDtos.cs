using Nawly.Shared.EnumHelper.PromotionEnums;

namespace Nawly.Shared.DTOs.Promotion;

public sealed record CouponDto(
    Guid Id,
    string Code,
    CouponType Type,
    decimal Value,
    decimal? MaxDiscount,
    decimal? MinOrderAmount,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? UsageLimitTotal,
    int? UsageLimitPerUser,
    Guid? StoreId,
    string? StoreName,
    CouponFundingSource FundedBy,
    bool IsActive);

public sealed record UpsertCouponDto(
    string Code,
    CouponType Type,
    decimal Value,
    decimal? MaxDiscount,
    decimal? MinOrderAmount,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? UsageLimitTotal,
    int? UsageLimitPerUser,
    Guid? StoreId,
    CouponFundingSource FundedBy,
    bool IsActive);

public sealed record CouponValidationDto(
    bool IsValid,
    string Code,
    decimal DiscountAmount,
    string? FailureReason = null);

public sealed record ValidateCouponRequestDto(
    Guid StoreId,
    decimal Subtotal,
    decimal DeliveryFee,
    string Code);

public sealed record BannerDto(
    Guid Id,
    string Title,
    string TitleAr,
    string ImageRef,
    BannerTargetType TargetType,
    Guid? TargetId,
    string? TargetUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int SortOrder,
    bool IsActive);

public sealed record UpsertBannerDto(
    string Title,
    string TitleAr,
    string ImageRef,
    BannerTargetType TargetType,
    Guid? TargetId,
    string? TargetUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int SortOrder,
    bool IsActive);
