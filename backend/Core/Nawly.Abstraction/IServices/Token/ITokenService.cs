using Nawly.Shared.DTOs.Auth.Token;
using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Abstraction.IServices.Token;

public sealed record IssuedTokensResult(
    AuthTokenDto Tokens,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public interface ITokenService
{
    Task<IssuedTokensResult> GenerateTokensAsync(
        Guid userId,
        string phoneNumber,
        string fullName,
        PreferredLanguage preferredLanguage,
        IReadOnlyCollection<string> roles,
        Guid? storeId = null,
        Guid? courierId = null,
        CancellationToken ct = default);

    string GenerateRefreshToken();
    string HashToken(string rawToken);
    string GenerateMfaChallengeToken(Guid userId);
    Guid? ValidateMfaChallengeToken(string challengeToken);
}
