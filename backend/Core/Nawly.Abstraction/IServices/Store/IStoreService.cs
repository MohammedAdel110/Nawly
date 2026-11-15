using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Store;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Store;

namespace Nawly.Abstraction.IServices.Store;

public interface IStoreService
{
    Task<Result<PaginationResponse<StoreSummaryDto>>> SearchStoresAsync(StoreQueryParams queryParams, CancellationToken ct = default);
    Task<Result<StoreDetailsDto>> GetStoreBySlugAsync(string slug, CancellationToken ct = default);
    Task<Result<StoreDetailsDto>> GetStoreByIdAsync(Guid storeId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<StoreCategoryDto>>> GetCategoriesAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<Result<StoreCategoryDto>> UpsertCategoryAsync(Guid? categoryId, UpsertStoreCategoryDto request, CancellationToken ct = default);
    Task<Result<StoreDetailsDto>> ApplyAsync(ApplyStoreDto request, CancellationToken ct = default);
    Task<Result<StoreDetailsDto>> UpdateSettingsAsync(Guid storeId, Guid actorUserId, UpdateStoreSettingsDto request, CancellationToken ct = default);
    Task<Result> UpdateOpeningHoursAsync(Guid storeId, Guid actorUserId, IReadOnlyList<StoreOpeningHourDto> hours, CancellationToken ct = default);
    Task<Result> PauseStoreAsync(Guid storeId, Guid actorUserId, PauseStoreDto request, CancellationToken ct = default);
    Task<Result> ResumeStoreAsync(Guid storeId, Guid actorUserId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<StoreStaffDto>>> GetStaffAsync(Guid storeId, CancellationToken ct = default);
    Task<Result<StoreStaffDto>> AddStaffAsync(Guid storeId, Guid actorUserId, AddStoreStaffDto request, CancellationToken ct = default);
    Task<Result> RemoveStaffAsync(Guid storeId, Guid actorUserId, Guid staffUserId, CancellationToken ct = default);
    Task<Result<StoreDashboardDto>> GetDashboardAsync(Guid storeId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<StoreSummaryDto>>> GetFavoritesAsync(Guid userId, CancellationToken ct = default);
    Task<Result> AddFavoriteAsync(Guid userId, Guid storeId, CancellationToken ct = default);
    Task<Result> RemoveFavoriteAsync(Guid userId, Guid storeId, CancellationToken ct = default);
}
