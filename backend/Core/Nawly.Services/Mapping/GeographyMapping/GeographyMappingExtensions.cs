using Nawly.Domain.Models.Geography;
using Nawly.Shared.DTOs.Geography;

namespace Nawly.Services.Mapping.GeographyMapping;

public static class GeographyMappingExtensions
{
    public static DeliveryZoneDto ToDto(this DeliveryZone zone) => new(
        zone.Id,
        zone.Name,
        zone.NameAr,
        zone.BaseFee,
        zone.PerKmFee,
        zone.MinOrderAmount,
        zone.FreeDeliveryThreshold,
        zone.EstimatedTravelMinutes,
        zone.IsActive);

    public static AreaDto ToDto(this Area area) => new(
        area.Id,
        area.ZoneId,
        area.Zone?.Name ?? string.Empty,
        area.Zone?.NameAr ?? string.Empty,
        area.Name,
        area.NameAr,
        area.SortOrder,
        area.IsActive);
}
