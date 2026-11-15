using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Catalog;

public class MenuCategory : BaseGuidEntity
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = [];
}
