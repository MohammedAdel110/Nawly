using Nawly.Domain.Models.Common;
using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Domain.Models.Identity;

public class OtpChallenge : BaseGuidEntity
{
    public const int MaxAllowedAttempts = 5;

    public string PhoneNumber { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public string? RequestIp { get; set; }

    public bool IsConsumed => ConsumedAt.HasValue;
    public bool IsLockedOut => Attempts >= MaxAllowedAttempts;
    public bool CanVerify(DateTimeOffset utcNow) => !IsConsumed && !IsLockedOut && utcNow < ExpiresAt;

    public void IncrementAttempts() => Attempts++;
    public void MarkConsumed(DateTimeOffset utcNow) => ConsumedAt = utcNow;
}
