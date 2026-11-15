using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Promotions;
using Nawly.Shared.Common.Params.Promotion;

namespace Nawly.Services.Specifications.PromotionSpec;

public sealed class CouponByCodeSpecification : BaseSpecifications<Coupon, Guid>
{
    public CouponByCodeSpecification(string normalizedCode)
        : base(c => !c.IsDeleted && c.Code.ToUpper() == normalizedCode)
    {
        AddInclude(c => c.Store!);
        ApplyAsNoTracking();
    }
}

public sealed class CouponsFilterSpecification : BaseSpecifications<Coupon, Guid>
{
    public CouponsFilterSpecification(CouponQueryParams queryParams)
    {
        var storeId = queryParams.StoreId;
        var activeOnly = queryParams.ActiveOnly;
        var search = queryParams.Search?.Trim().ToUpperInvariant();
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = c => !c.IsDeleted
            && (storeId == null || c.StoreId == storeId)
            && (activeOnly != true || c.IsActive)
            && (!hasSearch || c.Code.ToUpper().Contains(search!));

        AddInclude(c => c.Store!);
        AddOrderBy(c => c.CreatedAt, isDescending: true);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
    }
}

public sealed class CouponsCountSpecification : BaseSpecifications<Coupon, Guid>
{
    public CouponsCountSpecification(CouponQueryParams queryParams)
    {
        var storeId = queryParams.StoreId;
        var activeOnly = queryParams.ActiveOnly;
        var search = queryParams.Search?.Trim().ToUpperInvariant();
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = c => !c.IsDeleted
            && (storeId == null || c.StoreId == storeId)
            && (activeOnly != true || c.IsActive)
            && (!hasSearch || c.Code.ToUpper().Contains(search!));
    }
}

public sealed class CouponRedemptionsCountSpecification : BaseSpecifications<CouponRedemption, Guid>
{
    public CouponRedemptionsCountSpecification(Guid couponId, Guid? userId = null)
        : base(r => !r.IsDeleted && r.CouponId == couponId && (userId == null || r.UserId == userId))
    {
    }
}

public sealed class ActiveBannersSpecification : BaseSpecifications<Banner, Guid>
{
    public ActiveBannersSpecification(DateTimeOffset utcNow)
        : base(b => !b.IsDeleted && b.IsActive && b.StartsAt <= utcNow && b.EndsAt >= utcNow)
    {
        AddOrderBy(b => b.SortOrder);
        AddOrderBy(b => b.CreatedAt, isDescending: true);
        ApplyAsNoTracking();
    }
}
