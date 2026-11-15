using Nawly.Domain.Models.Common;

namespace Nawly.Domain.Models.Identity;

public class RefreshToken : BaseGuidEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid JwtId { get; set; }
    public Guid FamilyId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceName { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public AppUser User { get; set; } = null!;

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired(DateTimeOffset utcNow) => utcNow >= ExpiresAt;
    public bool IsActive(DateTimeOffset utcNow) => !IsRevoked && !IsExpired(utcNow);

    public void Revoke(string reason, DateTimeOffset utcNow, Guid? replacedByTokenId = null)
    {
        if (IsRevoked) return;
        RevokedAt = utcNow;
        RevokedReason = reason;
        ReplacedByTokenId = replacedByTokenId;
    }
}
