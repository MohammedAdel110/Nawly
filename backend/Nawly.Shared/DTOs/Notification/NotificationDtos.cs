using Nawly.Shared.EnumHelper.NotificationEnums;
using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Shared.DTOs.Notification;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    string? DataJson,
    NotificationChannelFlags Channels,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);

public sealed record WebPushSubscriptionDto(
    string Endpoint,
    string P256dh,
    string Auth,
    string? UserAgent = null);

public sealed record BroadcastNotificationDto(
    UserRole? TargetRole,
    string Title,
    string Body,
    NotificationChannelFlags Channels,
    string? DataJson = null);
