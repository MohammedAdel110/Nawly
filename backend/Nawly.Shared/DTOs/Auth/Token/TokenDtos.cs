using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Shared.DTOs.Auth.Token;

public sealed record AuthTokenDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    Guid UserId,
    string FullName,
    PreferredLanguage PreferredLanguage,
    IReadOnlyList<string> Roles,
    Guid? StoreId = null);

public sealed record SessionDto(
    Guid Id,
    string? DeviceName,
    string? UserAgent,
    string? CreatedByIp,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsCurrent);
