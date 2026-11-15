using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Nawly.Abstraction.IServices.Auth;

namespace Nawly.Web.Extensions;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public Guid? StoreId
    {
        get
        {
            var raw = Principal?.FindFirstValue("store_id");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            if (Principal is null)
                return Array.Empty<string>();

            return Principal.FindAll(ClaimTypes.Role)
                .Concat(Principal.FindAll("role"))
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool HasRole(string role)
        => !string.IsNullOrWhiteSpace(role) &&
           (Principal?.IsInRole(role) == true ||
            Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
}
