using Nawly.Shared.EnumHelper.StoreEnums;

namespace Nawly.Shared.Common.Params.Store;

public class StoreQueryParams : BaseQueryParam
{
    public Guid? AreaId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? OpenNow { get; set; }
    public string? Sort { get; set; } // rating, delivery_fee, min_order, eta
    public StoreStatus? Status { get; set; }
}
