namespace Nawly.Shared.EnumHelper.PaymentEnums;

public enum PaymentStatus
{
    Unpaid = 0,
    Pending = 1,
    Authorized = 2,
    Captured = 3,
    Paid = 4,
    Failed = 5,
    Cancelled = 6,
    Refunded = 7,
    PartiallyRefunded = 8
}
