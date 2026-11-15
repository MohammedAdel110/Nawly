using Microsoft.AspNetCore.Identity;
using Nawly.Domain.Contracts;
using Nawly.Shared.EnumHelper.UserEnums;

namespace Nawly.Domain.Models.Identity;

public class AppUser : IdentityUser<Guid>, IEntity<Guid>, IAuditable
{
    public AppUser()
    {
        Id = Guid.CreateVersion7();
    }

    public string FullName { get; set; } = string.Empty;
    public PreferredLanguage PreferredLanguage { get; set; } = PreferredLanguage.Ar;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public CustomerProfile? CustomerProfile { get; set; }
    public CourierProfile? CourierProfile { get; set; }
    public ICollection<Catalog.StoreStaff> StoreStaffAssignments { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
