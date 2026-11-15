using Nawly.Domain.Models.Common;
using Nawly.Domain.ValueObjects;

namespace Nawly.Domain.Models.Catalog;

public class Product : BaseGuidEntity
{
    public Guid StoreId { get; set; }
    public Guid MenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NormalizedSearchText { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string? ImageRef { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool SoldOutUntilNextOpening { get; set; }
    public int? StockQuantity { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public int? PrepMinutes { get; set; }
    public int SortOrder { get; set; }
    public uint RowVersion { get; set; }

    public Store Store { get; set; } = null!;
    public MenuCategory MenuCategory { get; set; } = null!;
    public ICollection<ProductOptionGroup> OptionGroups { get; set; } = [];

    public bool IsOrderable => IsAvailable && !IsDeleted && (!StockQuantity.HasValue || StockQuantity.Value > 0);

    public void RefreshNormalizedSearchText()
        => NormalizedSearchText = ArabicNormalizer.CombineAndNormalize(Name, NameAr, Description, DescriptionAr, Sku, Barcode);

    public void DecrementStock(int quantity)
    {
        if (!StockQuantity.HasValue) return;
        StockQuantity = Math.Max(0, StockQuantity.Value - quantity);
        if (StockQuantity == 0)
            IsAvailable = false;
    }
}
