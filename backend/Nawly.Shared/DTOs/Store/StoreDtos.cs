using Nawly.Shared.EnumHelper.StoreEnums;

namespace Nawly.Shared.DTOs.Store;

public sealed record StoreCategoryDto(Guid Id, string Name, string NameAr, string? IconRef, int SortOrder, bool IsActive);
public sealed record UpsertStoreCategoryDto(string Name, string NameAr, string? IconRef, int SortOrder, bool IsActive);

public sealed record StoreSummaryDto(
    Guid Id,
    string Slug,
    string Name,
    string NameAr,
    string CategoryName,
    string CategoryNameAr,
    string? LogoRef,
    decimal DeliveryFee,
    decimal MinOrderAmount,
    int EstimatedMinutes,
    decimal RatingAvg,
    int RatingCount,
    bool IsOpenNow,
    DeliveryMode DeliveryMode);

public sealed record StoreDetailsDto(
    Guid Id,
    string Slug,
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,
    Guid CategoryId,
    string CategoryName,
    string CategoryNameAr,
    string? LogoRef,
    string? CoverRef,
    string Phone,
    Guid AreaId,
    string AddressText,
    double? Latitude,
    double? Longitude,
    StoreStatus Status,
    bool IsAcceptingOrders,
    bool IsOpenNow,
    DateTimeOffset? PausedUntil,
    DeliveryMode DeliveryMode,
    decimal MinOrderAmount,
    int DefaultPrepMinutes,
    int BusyExtraMinutes,
    decimal RatingAvg,
    int RatingCount,
    IReadOnlyList<StoreOpeningHourDto> OpeningHours);

public sealed record StoreOpeningHourDto(DayOfWeek DayOfWeek, TimeOnly OpensAt, TimeOnly ClosesAt, bool CrossesMidnight);

public sealed record ApplyStoreDto(
    string OwnerFullName,
    string OwnerPhone,
    string OwnerPassword,
    string StoreName,
    string StoreNameAr,
    string? Description,
    string? DescriptionAr,
    Guid CategoryId,
    Guid AreaId,
    string AddressText,
    string StorePhone,
    double? Latitude,
    double? Longitude,
    DeliveryMode DeliveryMode,
    decimal MinOrderAmount,
    int DefaultPrepMinutes,
    IReadOnlyList<Guid> DeliveryZoneIds);

public sealed record UpdateStoreSettingsDto(
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,
    string Phone,
    string AddressText,
    double? Latitude,
    double? Longitude,
    DeliveryMode DeliveryMode,
    decimal MinOrderAmount,
    int DefaultPrepMinutes,
    int BusyExtraMinutes,
    bool IsAcceptingOrders);

public sealed record PauseStoreDto(int? DurationMinutes, bool UntilTomorrow = false);

public sealed record StoreStaffDto(Guid UserId, string FullName, string PhoneNumber, StoreRole StoreRole, bool IsActive);
public sealed record AddStoreStaffDto(string FullName, string PhoneNumber, string Password, StoreRole StoreRole);

public sealed record StoreDashboardDto(
    int TodayOrdersCount,
    decimal TodayRevenue,
    int PendingOrdersCount,
    int ActiveOrdersCount,
    int DefaultPrepMinutes,
    bool IsAcceptingOrders,
    DateTimeOffset? PausedUntil);
