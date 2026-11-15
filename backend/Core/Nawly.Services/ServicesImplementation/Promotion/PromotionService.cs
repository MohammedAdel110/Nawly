using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Promotion;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Promotions;
using Nawly.Services.Mapping.PromotionMapping;
using Nawly.Services.Specifications.PromotionSpec;
using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Promotion;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Promotion;

namespace Nawly.Services.ServicesImplementation.Promotion;

public sealed class PromotionService(
    IUnitOfWork unitOfWork,
    IClock clock) : IPromotionService
{
    public async Task<Result<CouponValidationDto>> ValidateCouponAsync(
        Guid userId,
        Guid storeId,
        decimal subtotal,
        decimal deliveryFee,
        string code,
        CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var coupon = await unitOfWork
            .GetRepository<Coupon, Guid>()
            .GetByIdWithSpecAsync(new CouponByCodeSpecification(normalized), ct);

        if (coupon is null)
            return Result.Success(new CouponValidationDto(false, normalized, 0m, "Coupon code not found."));

        var redemptionRepo = unitOfWork.GetRepository<CouponRedemption, Guid>();
        var totalRedemptions = await redemptionRepo.GetCountAsync(new CouponRedemptionsCountSpecification(coupon.Id), ct);
        var userRedemptions = await redemptionRepo.GetCountAsync(new CouponRedemptionsCountSpecification(coupon.Id, userId), ct);

        var calc = coupon.CalculateDiscount(subtotal, deliveryFee, storeId, totalRedemptions, userRedemptions, clock.UtcNow);
        if (calc.IsFailure)
            return Result.Success(new CouponValidationDto(false, coupon.Code, 0m, calc.Error.Message));

        return Result.Success(new CouponValidationDto(true, coupon.Code, calc.Value));
    }

    public async Task<Result<PaginationResponse<CouponDto>>> GetCouponsAsync(
        CouponQueryParams queryParams,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Coupon, Guid>();
        var items = await repo.GetAllWithSpecAsync(new CouponsFilterSpecification(queryParams), ct);
        var total = await repo.GetCountAsync(new CouponsCountSpecification(queryParams), ct);

        return Result.Success(new PaginationResponse<CouponDto>(
            queryParams.PageIndex,
            queryParams.PageSize,
            total,
            items.Select(c => c.ToDto()).ToList()));
    }

    public async Task<Result<CouponDto>> UpsertCouponAsync(
        Guid? couponId,
        UpsertCouponDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Coupon, Guid>();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        if (!couponId.HasValue)
        {
            var duplicate = await repo.GetByIdWithSpecAsync(new CouponByCodeSpecification(normalizedCode), ct);
            if (duplicate is not null)
                return Result.Failure<CouponDto>(Error.Conflict("promotion.duplicate_coupon", "A coupon with this code already exists."));

            var created = new Coupon
            {
                Code = normalizedCode,
                Type = request.Type,
                Value = request.Value,
                MaxDiscount = request.MaxDiscount,
                MinOrderAmount = request.MinOrderAmount,
                StartsAt = request.StartsAt,
                EndsAt = request.EndsAt,
                UsageLimitTotal = request.UsageLimitTotal,
                UsageLimitPerUser = request.UsageLimitPerUser,
                StoreId = request.StoreId,
                FundedBy = request.FundedBy,
                IsActive = request.IsActive,
                CreatedAt = clock.UtcNow
            };

            await repo.AddAsync(created, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Success(created.ToDto());
        }

        var coupon = await repo.GetByIdAsync(couponId.Value, ct);
        if (coupon is null || coupon.IsDeleted)
            return Result.Failure<CouponDto>(Error.NotFound("promotion.coupon_not_found", "Coupon not found."));

        coupon.Code = normalizedCode;
        coupon.Type = request.Type;
        coupon.Value = request.Value;
        coupon.MaxDiscount = request.MaxDiscount;
        coupon.MinOrderAmount = request.MinOrderAmount;
        coupon.StartsAt = request.StartsAt;
        coupon.EndsAt = request.EndsAt;
        coupon.UsageLimitTotal = request.UsageLimitTotal;
        coupon.UsageLimitPerUser = request.UsageLimitPerUser;
        coupon.StoreId = request.StoreId;
        coupon.FundedBy = request.FundedBy;
        coupon.IsActive = request.IsActive;
        coupon.UpdatedAt = clock.UtcNow;

        repo.Update(coupon);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(coupon.ToDto());
    }

    public async Task<Result<IReadOnlyList<BannerDto>>> GetActiveBannersAsync(CancellationToken ct = default)
    {
        var banners = await unitOfWork
            .GetRepository<Banner, Guid>()
            .GetAllWithSpecAsync(new ActiveBannersSpecification(clock.UtcNow), ct);

        return Result.Success<IReadOnlyList<BannerDto>>(banners.Select(b => b.ToDto()).ToList());
    }

    public async Task<Result<IReadOnlyList<BannerDto>>> GetAllBannersAsync(CancellationToken ct = default)
    {
        var banners = await unitOfWork
            .GetRepository<Banner, Guid>()
            .GetAllWithSpecAsync(new AllBannersSpecification(), ct);

        return Result.Success<IReadOnlyList<BannerDto>>(banners.Select(b => b.ToDto()).ToList());
    }

    public async Task<Result<BannerDto>> UpsertBannerAsync(
        Guid? bannerId,
        UpsertBannerDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Banner, Guid>();
        if (!bannerId.HasValue)
        {
            var created = new Banner
            {
                Title = request.Title.Trim(),
                TitleAr = request.TitleAr.Trim(),
                ImageRef = request.ImageRef,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                TargetUrl = request.TargetUrl,
                StartsAt = request.StartsAt,
                EndsAt = request.EndsAt,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive,
                CreatedAt = clock.UtcNow
            };
            await repo.AddAsync(created, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Success(created.ToDto());
        }

        var banner = await repo.GetByIdAsync(bannerId.Value, ct);
        if (banner is null || banner.IsDeleted)
            return Result.Failure<BannerDto>(Error.NotFound("promotion.banner_not_found", "Banner not found."));

        banner.Title = request.Title.Trim();
        banner.TitleAr = request.TitleAr.Trim();
        banner.ImageRef = request.ImageRef;
        banner.TargetType = request.TargetType;
        banner.TargetId = request.TargetId;
        banner.TargetUrl = request.TargetUrl;
        banner.StartsAt = request.StartsAt;
        banner.EndsAt = request.EndsAt;
        banner.SortOrder = request.SortOrder;
        banner.IsActive = request.IsActive;
        banner.UpdatedAt = clock.UtcNow;

        repo.Update(banner);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(banner.ToDto());
    }

    public async Task<Result> DeleteBannerAsync(Guid bannerId, CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<Banner, Guid>();
        var banner = await repo.GetByIdAsync(bannerId, ct);
        if (banner is null || banner.IsDeleted)
            return Result.Failure(Error.NotFound("promotion.banner_not_found", "Banner not found."));

        banner.IsDeleted = true;
        banner.DeletedAt = clock.UtcNow;
        repo.Update(banner);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
