namespace Nawly.Shared.Common.Params.Catalog;

public class ProductQueryParams : BaseQueryParam
{
    public Guid? StoreId { get; set; }
    public Guid? MenuCategoryId { get; set; }
    public bool? AvailableOnly { get; set; }
}
