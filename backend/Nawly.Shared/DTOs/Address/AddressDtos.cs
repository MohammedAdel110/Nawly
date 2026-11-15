namespace Nawly.Shared.DTOs.Address;

public sealed record AddressDto(
    Guid Id,
    string Label,
    Guid AreaId,
    string AreaName,
    string AreaNameAr,
    Guid ZoneId,
    string Description,
    string? Building,
    string? Floor,
    string? Apartment,
    double? Latitude,
    double? Longitude,
    string ContactPhone,
    string? Notes,
    bool IsDefault);

public sealed record CreateAddressDto(
    string Label,
    Guid AreaId,
    string Description,
    string? Building,
    string? Floor,
    string? Apartment,
    double? Latitude,
    double? Longitude,
    string ContactPhone,
    string? Notes,
    bool IsDefault = false);

public sealed record UpdateAddressDto(
    string Label,
    Guid AreaId,
    string Description,
    string? Building,
    string? Floor,
    string? Apartment,
    double? Latitude,
    double? Longitude,
    string ContactPhone,
    string? Notes,
    bool IsDefault);
