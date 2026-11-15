using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Catalog;

public class ProductOptionGroup : BaseGuidEntity
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int MinSelect { get; set; }
    public int MaxSelect { get; set; } = 1;
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
    public ICollection<ProductOption> Options { get; set; } = [];
}
