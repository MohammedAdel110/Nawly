using Nawly.Domain.Models.Geography;
using Nawly.Shared.DTOs.Address;

namespace Nawly.Services.Mapping.AddressMapping;

public static class AddressMappingExtensions
{
    public static AddressDto ToDto(this Address address) => new(
        address.Id,
        address.Label,
        address.AreaId,
        address.Area?.Name ?? string.Empty,
        address.Area?.NameAr ?? string.Empty,
        address.Area?.ZoneId ?? Guid.Empty,
        address.Description,
        address.Building,
        address.Floor,
        address.Apartment,
        address.Latitude,
        address.Longitude,
        address.ContactPhone,
        address.Notes,
        address.IsDefault);
}
