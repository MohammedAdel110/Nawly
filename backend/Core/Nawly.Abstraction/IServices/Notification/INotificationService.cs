using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Notification;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Notification;
using Nawly.Shared.EnumHelper.NotificationEnums;

namespace Nawly.Abstraction.IServices.Notification;

public interface INotificationService
{
    Task<Result<PaginationResponse<NotificationDto>>> GetUserNotificationsAsync(Guid userId, NotificationQueryParams queryParams, CancellationToken ct = default);
    Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task<Result> SaveWebPushSubscriptionAsync(Guid userId, WebPushSubscriptionDto request, CancellationToken ct = default);
    Task<Result> RemoveWebPushSubscriptionAsync(Guid userId, string endpoint, CancellationToken ct = default);
    Task<Result> SendToUserAsync(Guid userId, NotificationType type, string title, string body, NotificationChannelFlags channels, string? dataJson = null, CancellationToken ct = default);
    Task<Result<int>> BroadcastAsync(Guid adminUserId, BroadcastNotificationDto request, CancellationToken ct = default);
}
