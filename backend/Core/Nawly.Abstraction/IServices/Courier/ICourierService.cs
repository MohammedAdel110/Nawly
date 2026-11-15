using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Courier;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Courier;

namespace Nawly.Abstraction.IServices.Courier;

public interface ICourierService
{
    Task<Result<CourierProfileDto>> ApplyAsync(ApplyCourierDto request, CancellationToken ct = default);
    Task<Result<CourierProfileDto>> GetProfileByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<CourierProfileDto>> ToggleOnlineAsync(Guid userId, ToggleCourierOnlineDto request, CancellationToken ct = default);
    Task<Result> UpdateLocationAsync(Guid userId, CourierLocationUpdateDto request, CancellationToken ct = default);
    Task<Result<CourierEarningsDto>> GetEarningsAsync(Guid userId, DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken ct = default);
    Task<Result<PaginationResponse<CourierProfileDto>>> SearchCouriersAsync(CourierQueryParams queryParams, CancellationToken ct = default);
}
