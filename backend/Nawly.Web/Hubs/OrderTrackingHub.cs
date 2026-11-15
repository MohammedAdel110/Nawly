using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Nawly.Abstraction.IServices.Order;

namespace Nawly.Web.Hubs;

[Authorize]
public sealed class OrderTrackingHub(IOrderService orderService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (TryGetUserId(out var userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"customer:{userId}");

        await base.OnConnectedAsync();
    }

    public async Task JoinOrderGroup(Guid orderId)
    {
        if (!TryGetUserId(out var userId))
            throw new HubException("Authentication required.");

        var roles = Context.User?.FindAll(ClaimTypes.Role)
            .Concat(Context.User.FindAll("role"))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        Guid? storeId = Guid.TryParse(Context.User?.FindFirstValue("store_id"), out var parsedStoreId)
            ? parsedStoreId
            : null;

        var access = await orderService.GetOrderDetailsAsync(orderId, userId, roles, storeId, Context.ConnectionAborted);
        if (access.IsFailure)
            throw new HubException(access.Error.Message);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"order:{orderId}");
    }

    public Task LeaveOrderGroup(Guid orderId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order:{orderId}");

    private bool TryGetUserId(out Guid userId)
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
