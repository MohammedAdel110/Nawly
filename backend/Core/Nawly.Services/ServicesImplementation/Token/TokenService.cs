using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Token;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Identity;
using Nawly.Shared.DTOs.Auth.Token;
using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Services.ServicesImplementation.Token;

public sealed class TokenService(
    IConfiguration configuration,
    IClock clock,
    IUnitOfWork unitOfWork) : ITokenService
{
    private const string FallbackSecret = "Nawly-Dev-JWT-Signing-Key-Change-In-Production-2026!";

    public async Task<IssuedTokensResult> GenerateTokensAsync(
        Guid userId,
        string phoneNumber,
        string fullName,
        PreferredLanguage preferredLanguage,
        IReadOnlyCollection<string> roles,
        Guid? storeId = null,
        Guid? courierId = null,
        CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var accessMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var mins) && mins > 0 ? mins : 15;
        var refreshDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out var days) && days > 0 ? days : 14;

        var accessExpires = now.AddMinutes(accessMinutes);
        var refreshExpires = now.AddDays(refreshDays);
        var jti = Guid.CreateVersion7();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti.ToString()),
            new(JwtRegisteredClaimNames.PhoneNumber, phoneNumber),
            new(JwtRegisteredClaimNames.Name, fullName),
            new("preferred_language", preferredLanguage.ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (storeId.HasValue)
            claims.Add(new Claim("store_id", storeId.Value.ToString()));

        if (courierId.HasValue)
            claims.Add(new Claim("courier_id", courierId.Value.ToString()));

        var accessToken = WriteJwt(claims, now, accessExpires);
        var rawRefreshToken = GenerateRefreshToken();
        var refreshEntity = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(rawRefreshToken),
            JwtId = jti,
            FamilyId = Guid.CreateVersion7(),
            ExpiresAt = refreshExpires,
            CreatedAt = now
        };

        await unitOfWork.GetRepository<RefreshToken, Guid>().AddAsync(refreshEntity, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = new AuthTokenDto(
            accessToken,
            accessExpires,
            userId,
            fullName,
            preferredLanguage,
            roles.ToList(),
            storeId,
            rawRefreshToken,
            refreshExpires);

        return new IssuedTokensResult(dto, rawRefreshToken, refreshExpires);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string HashToken(string rawToken)
        => ServiceHelpers.HashSecretCode(rawToken);

    public string GenerateMfaChallengeToken(Guid userId)
    {
        var now = clock.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("mfa_challenge", "true")
        };

        return WriteJwt(claims, now, now.AddMinutes(5));
    }

    public Guid? ValidateMfaChallengeToken(string challengeToken)
    {
        if (string.IsNullOrWhiteSpace(challengeToken))
            return null;

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = GetIssuer(),
            ValidateAudience = true,
            ValidAudience = GetAudience(),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetSigningKey(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(15)
        };

        try
        {
            var principal = handler.ValidateToken(challengeToken, parameters, out _);
            if (!string.Equals(principal.FindFirst("mfa_challenge")?.Value, "true", StringComparison.Ordinal))
                return null;

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }

    private string WriteJwt(IEnumerable<Claim> claims, DateTimeOffset notBefore, DateTimeOffset expires)
    {
        var credentials = new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: GetIssuer(),
            audience: GetAudience(),
            claims: claims,
            notBefore: notBefore.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        var secret = configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
            secret = FallbackSecret;

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    private string GetIssuer() => configuration["Jwt:Issuer"] ?? "Nawly.Api";
    private string GetAudience() => configuration["Jwt:Audience"] ?? "Nawly.Clients";
}
