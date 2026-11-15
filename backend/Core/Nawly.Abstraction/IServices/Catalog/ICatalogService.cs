using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Catalog;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Catalog;

namespace Nawly.Abstraction.IServices.Catalog;

public interface ICatalogService
{
    Task<Result<StoreMenuDto>> GetStoreMenuAsync(Guid storeId, bool includeUnavailable = false, CancellationToken ct = default);
    Task<Result<PaginationResponse<ProductDto>>> SearchProductsAsync(ProductQueryParams queryParams, CancellationToken ct = default);
    Task<Result<MenuCategoryDto>> CreateMenuCategoryAsync(Guid storeId, UpsertMenuCategoryDto request, CancellationToken ct = default);
    Task<Result<MenuCategoryDto>> UpdateMenuCategoryAsync(Guid storeId, Guid categoryId, UpsertMenuCategoryDto request, CancellationToken ct = default);
    Task<Result> DeleteMenuCategoryAsync(Guid storeId, Guid categoryId, CancellationToken ct = default);
    Task<Result<ProductDto>> CreateProductAsync(Guid storeId, UpsertProductDto request, CancellationToken ct = default);
    Task<Result<ProductDto>> UpdateProductAsync(Guid storeId, Guid productId, UpsertProductDto request, CancellationToken ct = default);
    Task<Result> DeleteProductAsync(Guid storeId, Guid productId, CancellationToken ct = default);
    Task<Result<ProductDto>> ToggleProductAvailabilityAsync(Guid storeId, Guid productId, ToggleProductAvailabilityDto request, CancellationToken ct = default);
    Task<Result<ProductOptionGroupDto>> UpsertOptionGroupAsync(Guid storeId, Guid productId, Guid? groupId, UpsertProductOptionGroupDto request, CancellationToken ct = default);
    Task<Result> DeleteOptionGroupAsync(Guid storeId, Guid productId, Guid groupId, CancellationToken ct = default);
}
