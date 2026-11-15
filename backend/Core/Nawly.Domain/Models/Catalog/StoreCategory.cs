using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Catalog;

public class StoreCategory : BaseGuidEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? IconRef { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Store> Stores { get; set; } = [];
}
