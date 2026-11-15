namespace Nawly.Shared.DTOs.Geography;

public sealed record DeliveryZoneDto(
    Guid Id,
    string Name,
    string NameAr,
    decimal BaseFee,
    decimal? PerKmFee,
    decimal MinOrderAmount,
    decimal? FreeDeliveryThreshold,
    int EstimatedTravelMinutes,
    bool IsActive);

public sealed record UpsertDeliveryZoneDto(
    string Name,
    string NameAr,
    decimal BaseFee,
    decimal? PerKmFee,
    decimal MinOrderAmount,
    decimal? FreeDeliveryThreshold,
    int EstimatedTravelMinutes,
    bool IsActive,
    string? PolygonGeoJson = null);

public sealed record AreaDto(
    Guid Id,
    Guid ZoneId,
    string ZoneName,
    string ZoneNameAr,
    string Name,
    string NameAr,
    int SortOrder,
    bool IsActive);

public sealed record UpsertAreaDto(
    Guid ZoneId,
    string Name,
    string NameAr,
    int SortOrder,
    bool IsActive);
