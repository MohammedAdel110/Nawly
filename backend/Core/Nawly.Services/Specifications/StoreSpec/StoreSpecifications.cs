using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.ValueObjects;
using Nawly.Shared.Common.Params.Store;
using Nawly.Shared.EnumHelper.StoreEnums;

namespace Nawly.Services.Specifications.StoreSpec;

public sealed class StoresFilterSpecification : BaseSpecifications<Store, Guid>
{
    public StoresFilterSpecification(StoreQueryParams queryParams)
    {
        var normalizedSearch = ArabicNormalizer.Normalize(queryParams.Search);
        var hasSearch = !string.IsNullOrWhiteSpace(normalizedSearch);
        var status = queryParams.Status;
        var categoryId = queryParams.CategoryId;
        var areaId = queryParams.AreaId;

        Criteria = s => !s.IsDeleted
            && (status == null || s.Status == status)
            && (categoryId == null || s.CategoryId == categoryId)
            && (areaId == null || s.AreaId == areaId || s.DeliveryZones.Any(dz => !dz.IsDeleted && dz.Zone.Areas.Any(a => a.Id == areaId && a.IsActive)))
            && (!hasSearch || s.NormalizedSearchText.Contains(normalizedSearch));

        AddInclude(s => s.Category);
        AddInclude(s => s.Area);
        AddInclude("Area.Zone");
        AddInclude(s => s.OpeningHours);
        AddInclude("DeliveryZones.Zone");

        switch (queryParams.Sort?.ToLowerInvariant())
        {
            case "rating":
                AddOrderBy(s => s.RatingAvg, isDescending: true);
                break;
            case "min_order":
                AddOrderBy(s => s.MinOrderAmount);
                break;
            case "eta":
                AddOrderBy(s => s.DefaultPrepMinutes + s.BusyExtraMinutes);
                break;
            default:
                AddOrderBy(s => s.RatingAvg, isDescending: true);
                AddOrderBy(s => s.Name);
                break;
        }

        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
        ApplySplitQuery();
    }
}

public sealed class StoresCountSpecification : BaseSpecifications<Store, Guid>
{
    public StoresCountSpecification(StoreQueryParams queryParams)
    {
        var normalizedSearch = ArabicNormalizer.Normalize(queryParams.Search);
        var hasSearch = !string.IsNullOrWhiteSpace(normalizedSearch);
        var status = queryParams.Status;
        var categoryId = queryParams.CategoryId;
        var areaId = queryParams.AreaId;

        Criteria = s => !s.IsDeleted
            && (status == null || s.Status == status)
            && (categoryId == null || s.CategoryId == categoryId)
            && (areaId == null || s.AreaId == areaId || s.DeliveryZones.Any(dz => !dz.IsDeleted && dz.Zone.Areas.Any(a => a.Id == areaId && a.IsActive)))
            && (!hasSearch || s.NormalizedSearchText.Contains(normalizedSearch));
    }
}

public sealed class StoreWithDetailsSpecification : BaseSpecifications<Store, Guid>
{
    public StoreWithDetailsSpecification(Guid storeId, bool asNoTracking = true)
        : base(s => s.Id == storeId && !s.IsDeleted)
    {
        AddIncludes(asNoTracking);
    }

    public StoreWithDetailsSpecification(string slug, bool asNoTracking = true)
        : base(s => s.Slug == slug && !s.IsDeleted)
    {
        AddIncludes(asNoTracking);
    }

    private void AddIncludes(bool asNoTracking)
    {
        AddInclude(s => s.Category);
        AddInclude(s => s.Area);
        AddInclude("Area.Zone");
        AddInclude(s => s.OpeningHours);
        AddInclude("DeliveryZones.Zone");
        ApplySplitQuery();
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}

public sealed class StoreCategoriesSpecification : BaseSpecifications<StoreCategory, Guid>
{
    public StoreCategoriesSpecification(bool activeOnly)
        : base(c => !c.IsDeleted && (!activeOnly || c.IsActive))
    {
        AddOrderBy(c => c.SortOrder);
        AddOrderBy(c => c.Name);
        ApplyAsNoTracking();
    }
}

public sealed class StoreStaffSpecification : BaseSpecifications<StoreStaff, Guid>
{
    public StoreStaffSpecification(Guid storeId)
        : base(st => st.StoreId == storeId && !st.IsDeleted)
    {
        AddInclude(st => st.User);
        AddOrderBy(st => st.CreatedAt);
        ApplyAsNoTracking();
    }
}

public sealed class CustomerFavoritesSpecification : BaseSpecifications<Favorite, Guid>
{
    public CustomerFavoritesSpecification(Guid customerProfileId)
        : base(f => f.CustomerId == customerProfileId && !f.IsDeleted && !f.Store.IsDeleted && f.Store.Status == StoreStatus.Active)
    {
        AddInclude("Store.Category");
        AddInclude("Store.OpeningHours");
        AddInclude("Store.DeliveryZones.Zone");
        AddOrderBy(f => f.CreatedAt, isDescending: true);
        ApplyAsNoTracking();
        ApplySplitQuery();
    }
}
