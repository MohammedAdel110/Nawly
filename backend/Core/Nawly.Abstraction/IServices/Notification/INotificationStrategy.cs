using Nawly.Shared.Common.Results;
using Nawly.Shared.EnumHelper.NotificationEnums;

namespace Nawly.Abstraction.IServices.Notification;

public interface INotificationStrategy
{
    NotificationChannelFlags Channel { get; }

    Task<Result> SendAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string? dataJson,
        CancellationToken ct = default);
}
