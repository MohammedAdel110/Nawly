using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Catalog;

public class ProductOption : BaseGuidEntity
{
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int SortOrder { get; set; }

    public ProductOptionGroup Group { get; set; } = null!;
}
