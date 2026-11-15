using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Shared.DTOs.Courier;

public sealed record ApplyCourierDto(
    string FullName,
    string PhoneNumber,
    string Password,
    VehicleType VehicleType,
    string NationalIdImageRef,
    string? PhotoRef = null);

public sealed record CourierProfileDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string PhoneNumber,
    CourierStatus Status,
    VehicleType VehicleType,
    string? PhotoRef,
    bool IsOnline,
    double? LastLat,
    double? LastLng,
    DateTimeOffset? LastLocationAt,
    int MaxConcurrentDeliveries,
    decimal CashOnHand,
    decimal RatingAvg,
    int RatingCount,
    DateTimeOffset? LastDeliveredAt,
    string? RejectionReason);

public sealed record ToggleCourierOnlineDto(bool IsOnline);

public sealed record CourierLocationUpdateDto(
    double Latitude,
    double Longitude,
    double? Heading = null,
    double? Speed = null,
    DateTimeOffset? RecordedAt = null);

public sealed record CourierEarningsDto(
    int CompletedDeliveriesCount,
    decimal TotalPayouts,
    decimal PendingSettlementAmount,
    decimal CashOnHand,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd);

public sealed record CourierCashSummaryDto(
    Guid CourierId,
    string CourierName,
    decimal CashOnHand,
    int UnreconciledCashOrdersCount,
    DateTimeOffset? LastReconciledAt);
