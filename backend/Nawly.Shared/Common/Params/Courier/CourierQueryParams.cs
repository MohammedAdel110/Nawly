using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Shared.Common.Params.Courier;

public class CourierQueryParams : BaseQueryParam
{
    public CourierStatus? Status { get; set; }
    public bool? IsOnline { get; set; }
}
