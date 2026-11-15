using Nawly.Domain.Models.Identity;
using Nawly.Shared.DTOs.Admin;
using Nawly.Shared.DTOs.Profile;

namespace Nawly.Services.Mapping.ProfileMapping;

public static class ProfileMappingExtensions
{
    public static ProfileDto ToProfileDto(this AppUser user, IReadOnlyList<string> roles) => new(
        user.Id,
        user.PhoneNumber ?? string.Empty,
        user.FullName,
        user.Email,
        user.PreferredLanguage,
        user.Status,
        roles);

    public static AdminUserDto ToAdminUserDto(this AppUser user, IReadOnlyList<string> roles) => new(
        user.Id,
        user.FullName,
        user.PhoneNumber ?? string.Empty,
        user.Email,
        user.Status,
        roles,
        user.CreatedAt,
        user.LastLoginAt);
}
