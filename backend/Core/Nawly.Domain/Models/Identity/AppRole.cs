using Microsoft.AspNetCore.Identity;
using Nawly.Domain.Contracts;

namespace Nawly.Domain.Models.Identity;

public class AppRole : IdentityRole<Guid>, IEntity<Guid>
{
    public AppRole()
    {
        Id = Guid.CreateVersion7();
    }

    public AppRole(string roleName, string? description = null) : base(roleName)
    {
        Id = Guid.CreateVersion7();
        Description = description;
    }

    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
