namespace Nawly.Shared.DTOs.Auth.Login;

public sealed record LoginRequestDto(string Identifier, string Password, string? DeviceName = null);

public sealed record VerifyMfaRequestDto(string ChallengeToken, string Code, bool IsRecoveryCode = false, string? DeviceName = null);

public sealed record LoginResponseDto(
    bool RequiresMfa,
    string? MfaChallengeToken,
    Token.AuthTokenDto? Tokens);
