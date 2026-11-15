using Nawly.Domain.Models.Catalog;
using Nawly.Shared.DTOs.Catalog;

namespace Nawly.Services.Mapping.CatalogMapping;

public static class CatalogMappingExtensions
{
    public static MenuCategoryDto ToDto(this MenuCategory category) => new(
        category.Id,
        category.StoreId,
        category.Name,
        category.NameAr,
        category.SortOrder);

    public static ProductOptionDto ToDto(this ProductOption option) => new(
        option.Id,
        option.Name,
        option.NameAr,
        option.PriceDelta,
        option.IsAvailable,
        option.SortOrder);

    public static ProductOptionGroupDto ToDto(this ProductOptionGroup group) => new(
        group.Id,
        group.Name,
        group.NameAr,
        group.IsRequired,
        group.MinSelect,
        group.MaxSelect,
        group.SortOrder,
        group.Options
            .Where(o => !o.IsDeleted)
            .OrderBy(o => o.SortOrder)
            .Select(o => o.ToDto())
            .ToList());

    public static ProductDto ToDto(this Product product) => new(
        product.Id,
        product.StoreId,
        product.MenuCategoryId,
        product.Name,
        product.NameAr,
        product.Description,
        product.DescriptionAr,
        product.ImageRef,
        product.Price,
        product.CompareAtPrice,
        product.IsOrderable,
        product.StockQuantity,
        product.Sku,
        product.Barcode,
        product.PrepMinutes,
        product.SortOrder,
        product.OptionGroups
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.SortOrder)
            .Select(g => g.ToDto())
            .ToList());
}
