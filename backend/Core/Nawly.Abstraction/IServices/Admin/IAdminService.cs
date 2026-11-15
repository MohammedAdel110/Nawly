using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Admin;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Admin;
using Nawly.Shared.DTOs.Courier;
using Nawly.Shared.DTOs.Store;

namespace Nawly.Abstraction.IServices.Admin;

public interface IAdminService
{
    Task<Result<PlatformDashboardDto>> GetDashboardAsync(CancellationToken ct = default);
    Task<Result<PaginationResponse<AdminUserDto>>> GetUsersAsync(UserQueryParams queryParams, CancellationToken ct = default);
    Task<Result> SuspendUserAsync(Guid adminUserId, Guid targetUserId, SuspendUserDto request, CancellationToken ct = default);
    Task<Result> ActivateUserAsync(Guid adminUserId, Guid targetUserId, CancellationToken ct = default);
    Task<Result<StoreDetailsDto>> ReviewStoreApplicationAsync(Guid adminUserId, Guid storeId, ReviewApplicationDto request, CancellationToken ct = default);
    Task<Result<CourierProfileDto>> ReviewCourierApplicationAsync(Guid adminUserId, Guid courierId, ReviewApplicationDto request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SystemSettingDto>>> GetSystemSettingsAsync(CancellationToken ct = default);
    Task<Result<SystemSettingDto>> UpsertSystemSettingAsync(Guid adminUserId, SystemSettingDto request, CancellationToken ct = default);
    Task<Result<PaginationResponse<AuditLogDto>>> GetAuditLogsAsync(AuditLogQueryParams queryParams, CancellationToken ct = default);
}
