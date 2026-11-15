using Nawly.Domain.Models.Catalog;
using Nawly.Shared.DTOs.Store;

namespace Nawly.Services.Mapping.StoreMapping;

public static class StoreMappingExtensions
{
    public static StoreCategoryDto ToDto(this StoreCategory category) => new(
        category.Id,
        category.Name,
        category.NameAr,
        category.IconRef,
        category.SortOrder,
        category.IsActive);

    public static StoreOpeningHourDto ToDto(this StoreOpeningHour hour) => new(
        hour.DayOfWeek,
        hour.OpensAt,
        hour.ClosesAt,
        hour.CrossesMidnight);

    public static StoreStaffDto ToDto(this StoreStaff staff) => new(
        staff.UserId,
        staff.User?.FullName ?? string.Empty,
        staff.User?.PhoneNumber ?? string.Empty,
        staff.StoreRole,
        staff.IsActive);

    public static StoreSummaryDto ToSummaryDto(
        this Store store,
        TimeOnly cairoLocalTime,
        DayOfWeek cairoDayOfWeek,
        DateTimeOffset utcNow,
        Guid? targetZoneId = null)
    {
        var zoneMatch = targetZoneId.HasValue
            ? store.DeliveryZones.FirstOrDefault(dz => dz.ZoneId == targetZoneId.Value && !dz.IsDeleted)
            : store.DeliveryZones.FirstOrDefault(dz => !dz.IsDeleted);

        var deliveryFee = zoneMatch?.FeeOverride ?? zoneMatch?.Zone?.BaseFee ?? store.Area?.Zone?.BaseFee ?? 0m;
        var minOrder = zoneMatch?.MinOrderOverride ?? store.MinOrderAmount;
        var travelMinutes = zoneMatch?.Zone?.EstimatedTravelMinutes ?? store.Area?.Zone?.EstimatedTravelMinutes ?? 15;

        return new StoreSummaryDto(
            store.Id,
            store.Slug,
            store.Name,
            store.NameAr,
            store.Category?.Name ?? string.Empty,
            store.Category?.NameAr ?? string.Empty,
            store.LogoRef,
            deliveryFee,
            minOrder,
            store.EffectivePrepMinutes + travelMinutes,
            store.RatingAvg,
            store.RatingCount,
            store.IsCurrentlyOpen(cairoLocalTime, cairoDayOfWeek, utcNow),
            store.DeliveryMode);
    }

    public static StoreDetailsDto ToDetailsDto(
        this Store store,
        TimeOnly cairoLocalTime,
        DayOfWeek cairoDayOfWeek,
        DateTimeOffset utcNow) => new(
            store.Id,
            store.Slug,
            store.Name,
            store.NameAr,
            store.Description,
            store.DescriptionAr,
            store.CategoryId,
            store.Category?.Name ?? string.Empty,
            store.Category?.NameAr ?? string.Empty,
            store.LogoRef,
            store.CoverRef,
            store.Phone,
            store.AreaId,
            store.AddressText,
            store.Latitude,
            store.Longitude,
            store.Status,
            store.IsAcceptingOrders,
            store.IsCurrentlyOpen(cairoLocalTime, cairoDayOfWeek, utcNow),
            store.PausedUntil,
            store.DeliveryMode,
            store.MinOrderAmount,
            store.DefaultPrepMinutes,
            store.BusyExtraMinutes,
            store.RatingAvg,
            store.RatingCount,
            store.OpeningHours
                .Where(h => !h.IsDeleted)
                .OrderBy(h => h.DayOfWeek)
                .ThenBy(h => h.OpensAt)
                .Select(h => h.ToDto())
                .ToList());
}
