using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Shared.DTOs.Admin;

public sealed record AdminUserDto(
    Guid Id,
    string FullName,
    string PhoneNumber,
    string? Email,
    UserStatus Status,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);

public sealed record SuspendUserDto(string Reason);

public sealed record ReviewApplicationDto(
    bool Approved,
    string? RejectionReason = null,
    decimal? CommissionRateOverride = null);

public sealed record AssignCourierRequestDto(Guid CourierId, string Reason);

public sealed record SystemSettingDto(string Key, string Value, string Type, string? Description);

public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string Role,
    string Action,
    string EntityType,
    string EntityId,
    string? OldValuesJson,
    string? NewValuesJson,
    string? IpAddress,
    string? CorrelationId,
    DateTimeOffset At);

public sealed record PlatformDashboardDto(
    int TodayOrdersCount,
    decimal TodayGmv,
    int ActiveOrdersCount,
    int DelayedOrdersCount,
    int PendingStoreApplications,
    int PendingCourierApplications,
    int OnlineCouriersCount,
    int OpenSupportTicketsCount);
