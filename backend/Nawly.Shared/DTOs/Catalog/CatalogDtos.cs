namespace Nawly.Shared.DTOs.Catalog;

public sealed record MenuCategoryDto(Guid Id, Guid StoreId, string Name, string NameAr, int SortOrder);
public sealed record UpsertMenuCategoryDto(string Name, string NameAr, int SortOrder);

public sealed record ProductOptionDto(Guid Id, string Name, string NameAr, decimal PriceDelta, bool IsAvailable, int SortOrder);
public sealed record UpsertProductOptionDto(string Name, string NameAr, decimal PriceDelta, bool IsAvailable, int SortOrder);

public sealed record ProductOptionGroupDto(
    Guid Id,
    string Name,
    string NameAr,
    bool IsRequired,
    int MinSelect,
    int MaxSelect,
    int SortOrder,
    IReadOnlyList<ProductOptionDto> Options);

public sealed record UpsertProductOptionGroupDto(
    string Name,
    string NameAr,
    bool IsRequired,
    int MinSelect,
    int MaxSelect,
    int SortOrder,
    IReadOnlyList<UpsertProductOptionDto> Options);

public sealed record ProductDto(
    Guid Id,
    Guid StoreId,
    Guid MenuCategoryId,
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,
    string? ImageRef,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsAvailable,
    int? StockQuantity,
    string? Sku,
    string? Barcode,
    int? PrepMinutes,
    int SortOrder,
    IReadOnlyList<ProductOptionGroupDto> OptionGroups);

public sealed record UpsertProductDto(
    Guid MenuCategoryId,
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,
    string? ImageRef,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsAvailable,
    int? StockQuantity,
    string? Sku,
    string? Barcode,
    int? PrepMinutes,
    int SortOrder);

public sealed record ToggleProductAvailabilityDto(bool IsAvailable, bool SoldOutUntilNextOpening = false);

public sealed record StoreMenuDto(
    Guid StoreId,
    IReadOnlyList<MenuCategoryWithProductsDto> Categories);

public sealed record MenuCategoryWithProductsDto(
    Guid Id,
    string Name,
    string NameAr,
    int SortOrder,
    IReadOnlyList<ProductDto> Products);
