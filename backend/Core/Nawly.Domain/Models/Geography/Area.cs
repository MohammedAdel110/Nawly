using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Geography;

public class Area : BaseGuidEntity
{
    public Guid ZoneId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public DeliveryZone Zone { get; set; } = null!;
}
