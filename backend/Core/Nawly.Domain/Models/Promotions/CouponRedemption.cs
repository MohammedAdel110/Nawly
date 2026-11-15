using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Ordering;

namespace Nawly.Domain.Models.Promotions;

public class CouponRedemption : BaseGuidEntity
{
    public Guid CouponId { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset RedeemedAt { get; set; } = DateTimeOffset.UtcNow;

    public Coupon Coupon { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
