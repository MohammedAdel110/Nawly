namespace Nawly.Shared.Common.Params.Promotion;

public class CouponQueryParams : BaseQueryParam
{
    public Guid? StoreId { get; set; }
    public bool? ActiveOnly { get; set; }
}
