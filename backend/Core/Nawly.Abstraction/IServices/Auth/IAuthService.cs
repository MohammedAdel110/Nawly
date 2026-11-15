using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Auth.Login;
using Nawly.Shared.DTOs.Auth.Otp;
using Nawly.Shared.DTOs.Auth.Token;

namespace Nawly.Abstraction.IServices.Auth;

public interface IAuthService
{
    Task<Result<RequestOtpResponseDto>> RequestOtpAsync(RequestOtpDto request, CancellationToken ct = default);
    Task<Result<LoginResponseDto>> VerifyOtpAsync(VerifyOtpDto request, CancellationToken ct = default);
    Task<Result<LoginResponseDto>> LoginWithPasswordAsync(LoginRequestDto request, CancellationToken ct = default);
    Task<Result<LoginResponseDto>> VerifyMfaAsync(VerifyMfaRequestDto request, CancellationToken ct = default);
    Task<Result<AuthTokenDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<Result> LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default);
    Task<Result> LogoutAllSessionsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SessionDto>>> GetActiveSessionsAsync(Guid userId, CancellationToken ct = default);
    Task<Result> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
}
