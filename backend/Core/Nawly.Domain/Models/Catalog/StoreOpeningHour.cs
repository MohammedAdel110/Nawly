using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Catalog;

public class StoreOpeningHour : BaseGuidEntity
{
    public Guid StoreId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
    public bool CrossesMidnight { get; set; }

    public Store Store { get; set; } = null!;
}
