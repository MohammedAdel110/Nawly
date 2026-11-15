using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.ValueObjects;
using Nawly.Shared.Common.Params.Catalog;

namespace Nawly.Services.Specifications.CatalogSpec;

public sealed class StoreMenuCategoriesSpecification : BaseSpecifications<MenuCategory, Guid>
{
    public StoreMenuCategoriesSpecification(Guid storeId)
        : base(c => c.StoreId == storeId && !c.IsDeleted)
    {
        AddOrderBy(c => c.SortOrder);
        AddOrderBy(c => c.Name);
        ApplyAsNoTracking();
    }
}

public sealed class StoreProductsSpecification : BaseSpecifications<Product, Guid>
{
    public StoreProductsSpecification(Guid storeId, bool includeUnavailable)
        : base(p => p.StoreId == storeId && !p.IsDeleted && (includeUnavailable || p.IsAvailable))
    {
        AddInclude("OptionGroups.Options");
        AddOrderBy(p => p.SortOrder);
        AddOrderBy(p => p.Name);
        ApplyAsNoTracking();
        ApplySplitQuery();
    }
}

public sealed class ProductsFilterSpecification : BaseSpecifications<Product, Guid>
{
    public ProductsFilterSpecification(ProductQueryParams queryParams)
    {
        var normalizedSearch = ArabicNormalizer.Normalize(queryParams.Search);
        var hasSearch = !string.IsNullOrWhiteSpace(normalizedSearch);
        var storeId = queryParams.StoreId;
        var menuCategoryId = queryParams.MenuCategoryId;
        var availableOnly = queryParams.AvailableOnly;

        Criteria = p => !p.IsDeleted
            && (storeId == null || p.StoreId == storeId)
            && (menuCategoryId == null || p.MenuCategoryId == menuCategoryId)
            && (availableOnly != true || p.IsAvailable)
            && (!hasSearch || p.NormalizedSearchText.Contains(normalizedSearch));

        AddInclude("OptionGroups.Options");
        AddOrderBy(p => p.SortOrder);
        AddOrderBy(p => p.Name);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
        ApplySplitQuery();
    }
}

public sealed class ProductsCountSpecification : BaseSpecifications<Product, Guid>
{
    public ProductsCountSpecification(ProductQueryParams queryParams)
    {
        var normalizedSearch = ArabicNormalizer.Normalize(queryParams.Search);
        var hasSearch = !string.IsNullOrWhiteSpace(normalizedSearch);
        var storeId = queryParams.StoreId;
        var menuCategoryId = queryParams.MenuCategoryId;
        var availableOnly = queryParams.AvailableOnly;

        Criteria = p => !p.IsDeleted
            && (storeId == null || p.StoreId == storeId)
            && (menuCategoryId == null || p.MenuCategoryId == menuCategoryId)
            && (availableOnly != true || p.IsAvailable)
            && (!hasSearch || p.NormalizedSearchText.Contains(normalizedSearch));
    }
}

public sealed class ProductWithOptionsSpecification : BaseSpecifications<Product, Guid>
{
    public ProductWithOptionsSpecification(Guid productId, Guid? storeId = null, bool asNoTracking = false)
        : base(p => p.Id == productId && !p.IsDeleted && (storeId == null || p.StoreId == storeId))
    {
        AddInclude("OptionGroups.Options");
        ApplySplitQuery();
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}
