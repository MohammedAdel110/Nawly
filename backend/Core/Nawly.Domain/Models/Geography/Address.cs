using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.ValueObjects;

namespace Nawly.Domain.Models.Geography;

public class Address : BaseGuidEntity
{
    public const int MaxAddressesPerCustomer = 10;

    public Guid CustomerId { get; set; }
    public string Label { get; set; } = string.Empty;
    public Guid AreaId { get; set; }
    public string Description { get; set; } = string.Empty; // Landmark / street description (SRS §2.8)
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }

    public CustomerProfile Customer { get; set; } = null!;
    public Area Area { get; set; } = null!;

    public GeoPoint? GetGeoPoint() =>
        Latitude.HasValue && Longitude.HasValue ? new GeoPoint(Latitude.Value, Longitude.Value) : null;
}
