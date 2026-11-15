using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Shared.Common.Params.Delivery;

public class DeliveryQueryParams : BaseQueryParam
{
    public DeliveryStatus? Status { get; set; }
    public Guid? CourierId { get; set; }
}
