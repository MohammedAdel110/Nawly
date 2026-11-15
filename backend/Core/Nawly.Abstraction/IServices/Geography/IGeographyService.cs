using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Geography;

namespace Nawly.Abstraction.IServices.Geography;

public interface IGeographyService
{
    Task<Result<IReadOnlyList<DeliveryZoneDto>>> GetZonesAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AreaDto>>> GetAreasAsync(Guid? zoneId = null, bool activeOnly = true, CancellationToken ct = default);
    Task<Result<DeliveryZoneDto>> CreateZoneAsync(UpsertDeliveryZoneDto request, CancellationToken ct = default);
    Task<Result<DeliveryZoneDto>> UpdateZoneAsync(Guid zoneId, UpsertDeliveryZoneDto request, CancellationToken ct = default);
    Task<Result<AreaDto>> CreateAreaAsync(UpsertAreaDto request, CancellationToken ct = default);
    Task<Result<AreaDto>> UpdateAreaAsync(Guid areaId, UpsertAreaDto request, CancellationToken ct = default);
}
