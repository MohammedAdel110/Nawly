using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Sms;
using Nawly.Abstraction.IServices.Token;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.ValueObjects;
using Nawly.Services.Specifications.AuthSpec;
using Nawly.Services.Specifications.CourierSpec;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Auth.Login;
using Nawly.Shared.DTOs.Auth.Otp;
using Nawly.Shared.DTOs.Auth.Token;
using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Services.ServicesImplementation.Auth;

public sealed class AuthService(
    UserManager<AppUser> userManager,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    ISmsSender smsSender,
    IClock clock) : IAuthService
{
    private const int OtpCooldownSeconds = 60;
    private const int OtpExpiryMinutes = 5;

    public async Task<Result<RequestOtpResponseDto>> RequestOtpAsync(RequestOtpDto request, CancellationToken ct = default)
    {
        if (!PhoneNumber.TryParseEgyptianMobile(request.PhoneNumber, out var parsed) || parsed is null)
            return Result.Failure<RequestOtpResponseDto>(Error.Validation("auth.invalid_phone", "Phone number must be a valid Egyptian mobile number."));

        var normalized = parsed.Value;
        var otpRepo = unitOfWork.GetRepository<OtpChallenge, Guid>();
        var existing = await otpRepo.GetByIdWithSpecAsync(new LatestOtpChallengeSpecification(normalized, request.Purpose), ct);

        if (existing is not null && !existing.IsConsumed && (clock.UtcNow - existing.CreatedAt).TotalSeconds < OtpCooldownSeconds)
            return Result.Failure<RequestOtpResponseDto>(Error.Unprocessable("auth.otp_cooldown", "Please wait before requesting another verification code."));

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var challenge = new OtpChallenge
        {
            PhoneNumber = normalized,
            Purpose = request.Purpose,
            CodeHash = ServiceHelpers.HashSecretCode(code),
            ExpiresAt = clock.UtcNow.AddMinutes(OtpExpiryMinutes),
            CreatedAt = clock.UtcNow
        };

        await otpRepo.AddAsync(challenge, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await smsSender.SendAsync(
            normalized,
            "otp_login",
            new Dictionary<string, string> { ["code"] = code },
            request.Purpose.ToString(),
            ct);

        return Result.Success(new RequestOtpResponseDto(challenge.ExpiresAt, OtpCooldownSeconds));
    }

    public async Task<Result<LoginResponseDto>> VerifyOtpAsync(VerifyOtpDto request, CancellationToken ct = default)
    {
        if (!PhoneNumber.TryParseEgyptianMobile(request.PhoneNumber, out var parsed) || parsed is null)
            return Result.Failure<LoginResponseDto>(Error.Validation("auth.invalid_phone", "Phone number must be a valid Egyptian mobile number."));

        var normalized = parsed.Value;
        var otpRepo = unitOfWork.GetRepository<OtpChallenge, Guid>();
        var challenge = await otpRepo.GetByIdWithSpecAsync(new LatestOtpChallengeSpecification(normalized, request.Purpose), ct);

        if (challenge is null || !challenge.CanVerify(clock.UtcNow))
            return Result.Failure<LoginResponseDto>(Error.Unauthorized("auth.otp_invalid_or_expired", "Verification code is invalid, locked, or expired."));

        if (!ServiceHelpers.VerifySecretCode(request.Code, challenge.CodeHash))
        {
            challenge.IncrementAttempts();
            otpRepo.Update(challenge);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Failure<LoginResponseDto>(Error.Unauthorized("auth.otp_incorrect", "Incorrect verification code."));
        }

        challenge.MarkConsumed(clock.UtcNow);
        otpRepo.Update(challenge);

        var userRepo = unitOfWork.GetRepository<AppUser, Guid>();
        var user = await userRepo.GetByIdWithSpecAsync(new UserByPhoneOrEmailSpecification(normalized), ct);

        if (user is null)
        {
            user = new AppUser
            {
                UserName = normalized,
                PhoneNumber = normalized,
                PhoneNumberConfirmed = true,
                FullName = "Nawly User",
                PreferredLanguage = PreferredLanguage.Ar,
                Status = UserStatus.Active,
                LastLoginAt = clock.UtcNow,
                CreatedAt = clock.UtcNow
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return Result.Failure<LoginResponseDto>(Error.Validation("auth.user_creation_failed", string.Join("; ", createResult.Errors.Select(e => e.Description))));

            await userManager.AddToRoleAsync(user, RoleNames.Customer);
            await unitOfWork.GetRepository<CustomerProfile, Guid>().AddAsync(new CustomerProfile { UserId = user.Id }, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        else
        {
            if (user.Status != UserStatus.Active)
                return Result.Failure<LoginResponseDto>(Error.Forbidden("auth.user_suspended", "Your account is currently suspended."));

            user.LastLoginAt = clock.UtcNow;
            user.PhoneNumberConfirmed = true;
            await userManager.UpdateAsync(user);
            await unitOfWork.SaveChangesAsync(ct);
        }

        var issued = await IssueTokensForUserAsync(user, request.DeviceName, familyIdOverride: null, ct);
        return Result.Success(new LoginResponseDto(false, null, issued.Tokens));
    }

    public async Task<Result<LoginResponseDto>> LoginWithPasswordAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var identifier = PhoneNumber.TryParseEgyptianMobile(request.Identifier, out var parsed) && parsed is not null
            ? parsed.Value
            : request.Identifier.Trim();

        var user = await unitOfWork
            .GetRepository<AppUser, Guid>()
            .GetByIdWithSpecAsync(new UserByPhoneOrEmailSpecification(identifier), ct);

        user ??= await userManager.FindByEmailAsync(identifier);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Result.Failure<LoginResponseDto>(Error.Unauthorized("auth.invalid_credentials", "Invalid credentials."));

        if (user.Status != UserStatus.Active)
            return Result.Failure<LoginResponseDto>(Error.Forbidden("auth.user_suspended", "Your account is currently suspended."));

        if (user.TwoFactorEnabled)
        {
            var challengeToken = tokenService.GenerateMfaChallengeToken(user.Id);
            return Result.Success(new LoginResponseDto(true, challengeToken, null));
        }

        user.LastLoginAt = clock.UtcNow;
        await userManager.UpdateAsync(user);

        var issued = await IssueTokensForUserAsync(user, request.DeviceName, familyIdOverride: null, ct);
        return Result.Success(new LoginResponseDto(false, null, issued.Tokens));
    }

    public async Task<Result<LoginResponseDto>> VerifyMfaAsync(VerifyMfaRequestDto request, CancellationToken ct = default)
    {
        var userId = tokenService.ValidateMfaChallengeToken(request.ChallengeToken);
        if (!userId.HasValue)
            return Result.Failure<LoginResponseDto>(Error.Unauthorized("auth.invalid_mfa_challenge", "MFA challenge token is invalid or expired."));

        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
            return Result.Failure<LoginResponseDto>(Error.Unauthorized("auth.user_not_found", "User account not found."));

        if (user.Status != UserStatus.Active)
            return Result.Failure<LoginResponseDto>(Error.Forbidden("auth.user_suspended", "Your account is currently suspended."));

        bool verified;
        if (request.IsRecoveryCode)
        {
            var result = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, request.Code);
            verified = result.Succeeded;
        }
        else
        {
            verified = await userManager.VerifyTwoFactorTokenAsync(
                user,
                userManager.Options.Tokens.AuthenticatorTokenProvider,
                request.Code);
        }

        if (!verified)
            return Result.Failure<LoginResponseDto>(Error.Unauthorized("auth.invalid_mfa_code", "Invalid MFA verification code."));

        user.LastLoginAt = clock.UtcNow;
        await userManager.UpdateAsync(user);

        var issued = await IssueTokensForUserAsync(user, request.DeviceName, familyIdOverride: null, ct);
        return Result.Success(new LoginResponseDto(false, null, issued.Tokens));
    }

    public async Task<Result<AuthTokenDto>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Failure<AuthTokenDto>(Error.Unauthorized("auth.invalid_refresh_token", "Refresh token is required."));

        var tokenRepo = unitOfWork.GetRepository<RefreshToken, Guid>();
        var existing = await tokenRepo.GetByIdWithSpecAsync(new RefreshTokenByHashSpecification(tokenService.HashToken(refreshToken)), ct);

        if (existing is null)
            return Result.Failure<AuthTokenDto>(Error.Unauthorized("auth.invalid_refresh_token", "Invalid refresh token."));

        if (existing.IsRevoked)
        {
            // Token reuse detection: revoke the entire token family to contain session hijacking.
            var familyTokens = await tokenRepo.GetAllWithSpecAsync(new RefreshTokensByFamilyIdSpecification(existing.FamilyId), ct);
            foreach (var token in familyTokens)
            {
                token.Revoke("ReuseDetected", clock.UtcNow);
                tokenRepo.Update(token);
            }
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Failure<AuthTokenDto>(Error.Unauthorized("auth.refresh_token_reused", "Refresh token reuse detected. Please log in again."));
        }

        if (existing.IsExpired(clock.UtcNow))
            return Result.Failure<AuthTokenDto>(Error.Unauthorized("auth.refresh_token_expired", "Refresh token has expired."));

        if (existing.User.Status != UserStatus.Active)
            return Result.Failure<AuthTokenDto>(Error.Forbidden("auth.user_suspended", "Your account is currently suspended."));

        var issued = await IssueTokensForUserAsync(existing.User, existing.DeviceName, existing.FamilyId, ct);
        var replacement = await tokenRepo.GetByIdWithSpecAsync(new RefreshTokenByHashSpecification(tokenService.HashToken(issued.RefreshToken)), ct);

        existing.Revoke("Rotated", clock.UtcNow, replacement?.Id);
        tokenRepo.Update(existing);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(issued.Tokens);
    }

    public async Task<Result> LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Success();

        var tokenRepo = unitOfWork.GetRepository<RefreshToken, Guid>();
        var existing = await tokenRepo.GetByIdWithSpecAsync(new RefreshTokenByHashSpecification(tokenService.HashToken(refreshToken)), ct);
        if (existing is not null && existing.UserId == userId && !existing.IsRevoked)
        {
            existing.Revoke("UserLogout", clock.UtcNow);
            tokenRepo.Update(existing);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    public async Task<Result> LogoutAllSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var tokenRepo = unitOfWork.GetRepository<RefreshToken, Guid>();
        var active = await tokenRepo.GetAllWithSpecAsync(new ActiveRefreshTokensByUserIdSpecification(userId, clock.UtcNow), ct);
        foreach (var token in active)
        {
            token.Revoke("LogoutAllSessions", clock.UtcNow);
            tokenRepo.Update(token);
        }

        if (active.Count > 0)
            await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> GetActiveSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var active = await unitOfWork
            .GetRepository<RefreshToken, Guid>()
            .GetAllWithSpecAsync(new ActiveRefreshTokensByUserIdSpecification(userId, clock.UtcNow), ct);

        IReadOnlyList<SessionDto> result = active
            .Select(r => new SessionDto(r.Id, r.DeviceName, r.UserAgent, r.CreatedByIp, r.CreatedAt, r.ExpiresAt, false))
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var tokenRepo = unitOfWork.GetRepository<RefreshToken, Guid>();
        var session = await tokenRepo.GetByIdWithSpecAsync(new UserSessionByIdSpecification(sessionId, userId), ct);
        if (session is null)
            return Result.Failure(Error.NotFound("auth.session_not_found", "Session not found."));

        session.Revoke("SessionRevokedByUser", clock.UtcNow);
        tokenRepo.Update(session);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<IssuedTokensResult> IssueTokensForUserAsync(
        AppUser user,
        string? deviceName,
        Guid? familyIdOverride,
        CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var staff = await unitOfWork
            .GetRepository<StoreStaff, Guid>()
            .GetByIdWithSpecAsync(new ActiveStoreStaffByUserIdSpecification(user.Id), ct);

        var courier = await unitOfWork
            .GetRepository<CourierProfile, Guid>()
            .GetByIdWithSpecAsync(new CourierByUserIdSpecification(user.Id, asNoTracking: true), ct);

        var issued = await tokenService.GenerateTokensAsync(
            user.Id,
            user.PhoneNumber ?? string.Empty,
            user.FullName,
            user.PreferredLanguage,
            roles.ToList(),
            staff?.StoreId,
            courier?.Id,
            ct);

        if (!string.IsNullOrWhiteSpace(deviceName) || familyIdOverride.HasValue)
        {
            var tokenRepo = unitOfWork.GetRepository<RefreshToken, Guid>();
            var stored = await tokenRepo.GetByIdWithSpecAsync(new RefreshTokenByHashSpecification(tokenService.HashToken(issued.RefreshToken)), ct);
            if (stored is not null)
            {
                if (!string.IsNullOrWhiteSpace(deviceName))
                    stored.DeviceName = deviceName.Trim();
                if (familyIdOverride.HasValue)
                    stored.FamilyId = familyIdOverride.Value;
                tokenRepo.Update(stored);
                await unitOfWork.SaveChangesAsync(ct);
            }
        }

        return issued;
    }
}
