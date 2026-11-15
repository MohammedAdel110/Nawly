using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Shared.Common.Params.Order;

public class OrderQueryParams : BaseQueryParam
{
    public OrderStatus? Status { get; set; }
    public Guid? StoreId { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}
