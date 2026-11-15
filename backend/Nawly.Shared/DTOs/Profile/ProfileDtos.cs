using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Shared.DTOs.Profile;

public sealed record ProfileDto(
    Guid Id,
    string PhoneNumber,
    string FullName,
    string? Email,
    PreferredLanguage PreferredLanguage,
    UserStatus Status,
    IReadOnlyList<string> Roles);

public sealed record UpdateProfileDto(string FullName, string? Email, PreferredLanguage PreferredLanguage);

public sealed record UserDataExportDto(
    ProfileDto Profile,
    IReadOnlyList<Address.AddressDto> Addresses,
    int CompletedOrdersCount,
    DateTimeOffset ExportedAt);
