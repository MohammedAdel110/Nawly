using Nawly.Domain.Models.Common;
using Nawly.Domain.ValueObjects;
using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Domain.Models.Identity;

public class CourierProfile : BaseGuidEntity
{
    public Guid UserId { get; set; }
    public CourierStatus Status { get; set; } = CourierStatus.Pending;
    public VehicleType VehicleType { get; set; } = VehicleType.Motorcycle;
    public string NationalIdImageRef { get; set; } = string.Empty;
    public string? PhotoRef { get; set; }
    public bool IsOnline { get; set; }
    public double? LastLat { get; set; }
    public double? LastLng { get; set; }
    public DateTimeOffset? LastLocationAt { get; set; }
    public int MaxConcurrentDeliveries { get; set; } = 1;
    public decimal CashOnHand { get; set; }
    public decimal RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public DateTimeOffset? LastDeliveredAt { get; set; }
    public string? RejectionReason { get; set; }

    public AppUser User { get; set; } = null!;

    public GeoPoint? GetLastLocation() =>
        LastLat.HasValue && LastLng.HasValue ? new GeoPoint(LastLat.Value, LastLng.Value) : null;

    public void UpdateLocation(double latitude, double longitude, DateTimeOffset utcNow)
    {
        var point = new GeoPoint(latitude, longitude);
        LastLat = point.Latitude;
        LastLng = point.Longitude;
        LastLocationAt = utcNow;
    }

    public void AddCashOnHand(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        CashOnHand = decimal.Round(CashOnHand + amount, 2, MidpointRounding.AwayFromZero);
    }

    public void DeductCashOnHand(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        CashOnHand = decimal.Round(Math.Max(0m, CashOnHand - amount), 2, MidpointRounding.AwayFromZero);
    }
}
