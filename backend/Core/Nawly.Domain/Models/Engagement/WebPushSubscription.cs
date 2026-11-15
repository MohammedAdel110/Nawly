using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Identity;

namespace Nawly.Domain.Models.Engagement;

public class WebPushSubscription : BaseGuidEntity
{
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }

    public AppUser User { get; set; } = null!;
}
