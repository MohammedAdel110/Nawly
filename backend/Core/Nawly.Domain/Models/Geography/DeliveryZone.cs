using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Geography;

public class DeliveryZone : BaseGuidEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public decimal BaseFee { get; set; }
    public decimal? PerKmFee { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal? FreeDeliveryThreshold { get; set; }
    public int EstimatedTravelMinutes { get; set; } = 15;
    public bool IsActive { get; set; } = true;
    public string? PolygonGeoJson { get; set; }

    public ICollection<Area> Areas { get; set; } = [];
    public ICollection<Catalog.StoreDeliveryZone> StoreDeliveryZones { get; set; } = [];
}
