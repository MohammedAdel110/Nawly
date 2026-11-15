using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;

namespace Nawly.Domain.Models.Delivery;

public class CourierLocationHistory : BaseGuidEntity
{
    public Guid CourierId { get; set; }
    public Guid? DeliveryId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;

    public CourierProfile Courier { get; set; } = null!;
}
