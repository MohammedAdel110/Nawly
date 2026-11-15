using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;
using Nawly.Shared.EnumHelper.NotificationEnums;

namespace Nawly.Domain.Models.Engagement;

public class Notification : BaseGuidEntity
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public NotificationChannelFlags Channels { get; set; } = NotificationChannelFlags.InApp;
    public DateTimeOffset? ReadAt { get; set; }

    public AppUser User { get; set; } = null!;

    public bool IsRead => ReadAt.HasValue;

    public void MarkAsRead(DateTimeOffset utcNow)
    {
        ReadAt ??= utcNow;
    }
}
