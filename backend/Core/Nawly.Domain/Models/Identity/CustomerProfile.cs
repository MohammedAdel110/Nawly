using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Geography;

namespace Nawly.Domain.Models.Identity;

public class CustomerProfile : BaseGuidEntity
{
    public Guid UserId { get; set; }
    public Guid? DefaultAddressId { get; set; }
    public int TrustScore { get; set; } = 100;
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int NoShowCount { get; set; }
    public bool IsCodBlocked { get; set; }

    public AppUser User { get; set; } = null!;
    public ICollection<Address> Addresses { get; set; } = [];

    public bool IsNewAccountForCodCap(int minDeliveredOrders = 3) => CompletedOrders < minDeliveredOrders;

    public void RecordDeliveryCompleted()
    {
        CompletedOrders++;
        TrustScore = Math.Min(100, TrustScore + 2);
    }

    public void RecordLateCancellation()
    {
        CancelledOrders++;
        TrustScore = Math.Max(0, TrustScore - 5);
    }

    public void RecordNoShow(int autoBlockThreshold = 3)
    {
        NoShowCount++;
        TrustScore = Math.Max(0, TrustScore - 20);
        if (NoShowCount >= autoBlockThreshold)
            IsCodBlocked = true;
    }
}
