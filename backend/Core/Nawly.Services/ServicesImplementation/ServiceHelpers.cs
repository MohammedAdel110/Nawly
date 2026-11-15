using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Platform;
using Nawly.Services.Specifications.AdminSpec;
using Nawly.Services.Specifications.OrderSpec;

namespace Nawly.Services.ServicesImplementation;

internal static class ServiceHelpers
{
    private static readonly PasswordHasher<AppUser> PasswordHasher = new();

    public static async Task<CustomerProfile> ResolveOrCreateCustomerProfileAsync(
        IUnitOfWork unitOfWork,
        Guid userId,
        CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<CustomerProfile, Guid>();
        var existing = await repo.GetByIdWithSpecAsync(new CustomerProfileByUserIdSpecification(userId), ct);
        if (existing is not null)
            return existing;

        var profile = new CustomerProfile { UserId = userId };
        await repo.AddAsync(profile, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return profile;
    }

    public static async Task<CustomerProfile?> ResolveCustomerProfileAsync(
        IUnitOfWork unitOfWork,
        Guid userId,
        CancellationToken ct)
    {
        var repo = unitOfWork.GetRepository<CustomerProfile, Guid>();
        return await repo.GetByIdWithSpecAsync(new CustomerProfileByUserIdSpecification(userId), ct);
    }

    public static string HashPassword(AppUser user, string password)
        => PasswordHasher.HashPassword(user, password);

    public static string HashSecretCode(string rawCode)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawCode.Trim()));
        return Convert.ToHexStringLower(bytes);
    }

    public static bool VerifySecretCode(string rawCode, string? hash)
    {
        if (string.IsNullOrWhiteSpace(rawCode) || string.IsNullOrWhiteSpace(hash))
            return false;

        var computed = HashSecretCode(rawCode);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(hash));
    }

    public static string GenerateSlug(string source)
    {
        var sb = new StringBuilder(source.Length);
        var prevHyphen = false;

        foreach (var ch in source.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                sb.Append(ch);
                prevHyphen = false;
            }
            else if (!prevHyphen && sb.Length > 0)
            {
                sb.Append('-');
                prevHyphen = true;
            }
        }

        var clean = sb.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(clean))
            clean = "store";

        var suffix = Guid.NewGuid().ToString("N")[..6];
        return $"{clean}-{suffix}";
    }

    public static async Task<decimal> GetDecimalSettingAsync(
        IUnitOfWork unitOfWork,
        string key,
        decimal fallback,
        CancellationToken ct)
    {
        var setting = await unitOfWork
            .GetRepository<SystemSetting, Guid>()
            .GetByIdWithSpecAsync(new SystemSettingsSpecification(key), ct);

        return setting is not null && decimal.TryParse(setting.Value, out var parsed)
            ? parsed
            : fallback;
    }

    public static async Task<bool> GetBoolSettingAsync(
        IUnitOfWork unitOfWork,
        string key,
        bool fallback,
        CancellationToken ct)
    {
        var setting = await unitOfWork
            .GetRepository<SystemSetting, Guid>()
            .GetByIdWithSpecAsync(new SystemSettingsSpecification(key), ct);

        return setting is not null && bool.TryParse(setting.Value, out var parsed)
            ? parsed
            : fallback;
    }
}

internal sealed class UserByPhoneSpecification(string e164Phone)
    : BaseSpecifications<AppUser, Guid>(u => u.PhoneNumber == e164Phone);
