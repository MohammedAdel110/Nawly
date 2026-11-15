using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Auth.Login;
using Nawly.Shared.DTOs.Auth.Otp;
using Nawly.Shared.DTOs.Auth.Token;

namespace Nawly.Presentation.Controllers.Auth;

[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ApiBaseController
{
    private const string RefreshTokenCookieName = "nawly_refresh_token";

    [AllowAnonymous]
    [EnableRateLimiting("auth-otp")]
    [HttpPost("otp/request")]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpDto request, CancellationToken ct)
    {
        var result = await authService.RequestOtpAsync(request, ct);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [HttpPost("otp/verify")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto request, CancellationToken ct)
    {
        var result = await authService.VerifyOtpAsync(request, ct);
        if (result.IsSuccess && result.Value.Tokens is { RefreshToken: not null } tokens)
            AppendRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiresAt);

        return HandleResult(result);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [HttpPost("login/password")]
    public async Task<IActionResult> LoginWithPassword([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await authService.LoginWithPasswordAsync(request, ct);
        if (result.IsSuccess && result.Value.Tokens is { RefreshToken: not null } tokens)
            AppendRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiresAt);

        return HandleResult(result);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [HttpPost("mfa/verify")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequestDto request, CancellationToken ct)
    {
        var result = await authService.VerifyMfaAsync(request, ct);
        if (result.IsSuccess && result.Value.Tokens is { RefreshToken: not null } tokens)
            AppendRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiresAt);

        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpPost("token/refresh")]
    public async Task<IActionResult> RefreshToken(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequestDto? request,
        CancellationToken ct)
    {
        var refreshToken = !string.IsNullOrWhiteSpace(request?.RefreshToken)
            ? request.RefreshToken
            : Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrWhiteSpace(refreshToken))
            return HandleResult(Result.Failure<AuthTokenDto>(Error.Unauthorized("auth.refresh_token_missing", "Refresh token is required.")));

        var result = await authService.RefreshTokenAsync(refreshToken, ct);
        if (result.IsSuccess && result.Value.RefreshToken is not null)
            AppendRefreshTokenCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiresAt);

        return HandleResult(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LogoutRequestDto? request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var refreshToken = !string.IsNullOrWhiteSpace(request?.RefreshToken)
            ? request.RefreshToken
            : Request.Cookies[RefreshTokenCookieName];

        var result = await authService.LogoutAsync(userId, refreshToken, ct);
        if (result.IsSuccess)
            DeleteRefreshTokenCookie();

        return HandleResult(result);
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAllSessions(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await authService.LogoutAllSessionsAsync(userId, ct);
        if (result.IsSuccess)
            DeleteRefreshTokenCookie();

        return HandleResult(result);
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<IActionResult> GetActiveSessions(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await authService.GetActiveSessionsAsync(userId, ct);
        return HandleResult(result);
    }

    [Authorize]
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await authService.RevokeSessionAsync(userId, sessionId, ct);
        return HandleResult(result);
    }

    private void AppendRefreshTokenCookie(string refreshToken, DateTimeOffset? expiresAt)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAt ?? DateTimeOffset.UtcNow.AddDays(30),
                Path = "/api/v1/auth"
            });
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/api/v1/auth"
            });
    }
}
