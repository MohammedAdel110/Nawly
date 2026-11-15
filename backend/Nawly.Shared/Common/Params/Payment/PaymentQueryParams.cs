using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Shared.Common.Params.Payment;

public class PaymentQueryParams : BaseQueryParam
{
    public Guid? OrderId { get; set; }
    public PaymentStatus? Status { get; set; }
    public PaymentProvider? Provider { get; set; }
}
