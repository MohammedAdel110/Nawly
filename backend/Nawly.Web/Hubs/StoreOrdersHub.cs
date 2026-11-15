using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nawly.Web.Hubs;

[Authorize(Policy = "VendorAccess")]
public sealed class StoreOrdersHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var rawStoreId = Context.User?.FindFirstValue("store_id");
        if (Guid.TryParse(rawStoreId, out var storeId) && storeId != Guid.Empty)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"store:{storeId}");

        await base.OnConnectedAsync();
    }

    public async Task JoinStoreGroup(Guid storeId)
    {
        if (storeId == Guid.Empty)
            throw new HubException("Invalid store id.");

        if (Context.User?.IsInRole("Admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"store:{storeId}");
            return;
        }

        var rawStoreId = Context.User?.FindFirstValue("store_id");
        if (!Guid.TryParse(rawStoreId, out var tokenStoreId) || tokenStoreId != storeId)
            throw new HubException("Forbidden store group access.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"store:{storeId}");
    }
}
