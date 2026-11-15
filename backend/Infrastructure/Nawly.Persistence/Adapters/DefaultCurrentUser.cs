using Nawly.Abstraction.IServices.Auth;

namespace Nawly.Persistence.Adapters;

/// <summary>
/// Fallback <see cref="ICurrentUser"/> when no HTTP request context is registered
/// (e.g., background workers, design-time tooling, or startup seeders).
/// </summary>
public sealed class DefaultCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
    public Guid? StoreId => null;
    public IReadOnlyCollection<string> Roles { get; } = Array.Empty<string>();
    public bool IsAuthenticated => false;
    public bool HasRole(string role) => false;
}
