using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Geography;

namespace Nawly.Domain.Models.Catalog;

public class StoreDeliveryZone : BaseGuidEntity
{
    public Guid StoreId { get; set; }
    public Guid ZoneId { get; set; }
    public decimal? FeeOverride { get; set; }
    public decimal? MinOrderOverride { get; set; }

    public Store Store { get; set; } = null!;
    public DeliveryZone Zone { get; set; } = null!;
}
