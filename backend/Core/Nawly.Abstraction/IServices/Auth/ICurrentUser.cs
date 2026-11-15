namespace Nawly.Abstraction.IServices.Auth;

/// <summary>
/// Represents the authenticated caller extracted from HTTP/SignalR execution context.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? StoreId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool HasRole(string role);
}
