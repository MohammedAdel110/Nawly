using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Geography;
using Nawly.Domain.ValueObjects;
using Nawly.Shared.EnumHelper.StoreEnums;

namespace Nawly.Domain.Models.Catalog;

public class Store : AggregateRoot
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NormalizedSearchText { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public Guid CategoryId { get; set; }
    public string? LogoRef { get; set; }
    public string? CoverRef { get; set; }
    public string Phone { get; set; } = string.Empty;
    public Guid AreaId { get; set; }
    public string AddressText { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public StoreStatus Status { get; set; } = StoreStatus.Pending;
    public bool IsAcceptingOrders { get; set; } = true;
    public DateTimeOffset? PausedUntil { get; set; }
    public DeliveryMode DeliveryMode { get; set; } = DeliveryMode.PlatformCourier;
    public decimal MinOrderAmount { get; set; }
    public int DefaultPrepMinutes { get; set; } = 20;
    public int BusyExtraMinutes { get; set; }
    public decimal? CommissionRate { get; set; }
    public decimal RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
    public uint RowVersion { get; set; }

    public StoreCategory Category { get; set; } = null!;
    public Area Area { get; set; } = null!;
    public ICollection<StoreStaff> Staff { get; set; } = [];
    public ICollection<StoreOpeningHour> OpeningHours { get; set; } = [];
    public ICollection<StoreDeliveryZone> DeliveryZones { get; set; } = [];
    public ICollection<MenuCategory> MenuCategories { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];

    public int EffectivePrepMinutes => DefaultPrepMinutes + BusyExtraMinutes;

    public GeoPoint? GetGeoPoint() =>
        Latitude.HasValue && Longitude.HasValue ? new GeoPoint(Latitude.Value, Longitude.Value) : null;

    public void RefreshNormalizedSearchText()
        => NormalizedSearchText = ArabicNormalizer.CombineAndNormalize(Name, NameAr, Description, DescriptionAr);

    /// <summary>
    /// Evaluates store availability in Africa/Cairo local time including midday breaks and overnight shifts (SRS §2.8, §4.5).
    /// </summary>
    public bool IsCurrentlyOpen(TimeOnly cairoLocalTime, DayOfWeek cairoDayOfWeek, DateTimeOffset utcNow)
    {
        if (Status != StoreStatus.Active || !IsAcceptingOrders || IsDeleted)
            return false;

        if (PausedUntil.HasValue && PausedUntil.Value > utcNow)
            return false;

        if (OpeningHours.Count == 0)
            return false;

        var previousDay = (DayOfWeek)(((int)cairoDayOfWeek + 6) % 7);

        return OpeningHours.Any(h =>
            (!h.CrossesMidnight && h.DayOfWeek == cairoDayOfWeek && cairoLocalTime >= h.OpensAt && cairoLocalTime <= h.ClosesAt) ||
            (h.CrossesMidnight && h.DayOfWeek == cairoDayOfWeek && cairoLocalTime >= h.OpensAt) ||
            (h.CrossesMidnight && h.DayOfWeek == previousDay && cairoLocalTime <= h.ClosesAt));
    }

    public void UpdateRatingAggregate(int newRating)
    {
        var total = (RatingAvg * RatingCount) + newRating;
        RatingCount++;
        RatingAvg = decimal.Round(total / RatingCount, 2, MidpointRounding.AwayFromZero);
    }
}
