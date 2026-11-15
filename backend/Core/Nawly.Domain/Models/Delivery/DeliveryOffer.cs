using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;
using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Domain.Models.Delivery;

public class DeliveryOffer : BaseGuidEntity
{
    public Guid DeliveryId { get; set; }
    public Guid CourierId { get; set; }
    public int Wave { get; set; }
    public DateTimeOffset OfferedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public DeliveryOfferResponse Response { get; set; } = DeliveryOfferResponse.Pending;

    public Delivery Delivery { get; set; } = null!;
    public CourierProfile Courier { get; set; } = null!;

    public bool IsStillValid(DateTimeOffset utcNow)
        => Response == DeliveryOfferResponse.Pending && utcNow < ExpiresAt;
}
