using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Nawly.Abstraction.IServices.Courier;
using Nawly.Shared.DTOs.Courier;

namespace Nawly.Web.Hubs;

[Authorize]
public sealed class DispatchHub(ICourierService courierService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var rawCourierId = Context.User?.FindFirstValue("courier_id");
        if (Guid.TryParse(rawCourierId, out var courierId) && courierId != Guid.Empty)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"courier:{courierId}");

        if (Context.User?.IsInRole("Admin") == true)
            await Groups.AddToGroupAsync(Context.ConnectionId, "admin:dispatch");

        await base.OnConnectedAsync();
    }

    public async Task UpdateLocation(CourierLocationUpdateDto request)
    {
        if (Context.User?.IsInRole("Courier") != true)
            throw new HubException("Only couriers can stream location updates.");

        var rawUserId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? Context.User.FindFirstValue("sub");
        if (!Guid.TryParse(rawUserId, out var userId) || userId == Guid.Empty)
            throw new HubException("Authentication required.");

        var result = await courierService.UpdateLocationAsync(userId, request, Context.ConnectionAborted);
        if (result.IsFailure)
            throw new HubException(result.Error.Message);
    }
}
