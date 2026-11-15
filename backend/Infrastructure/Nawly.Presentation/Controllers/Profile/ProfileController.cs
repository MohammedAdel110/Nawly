using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nawly.Abstraction.IServices.Profile;
using Nawly.Abstraction.IServices.Support;
using Nawly.Shared.Common.Params.Support;
using Nawly.Shared.DTOs.Profile;
using Nawly.Shared.DTOs.Support;

namespace Nawly.Presentation.Controllers.Profile;

[Authorize]
[Route("api/v1/profile")]
public sealed class ProfileController(
    IProfileService profileService,
    ISupportService supportService) : ApiBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await profileService.GetProfileAsync(userId, ct);
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await profileService.UpdateProfileAsync(userId, request, ct);
        return HandleResult(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportData(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await profileService.ExportUserDataAsync(userId, ct);
        return HandleResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> RequestAccountDeletion(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await profileService.RequestAccountDeletionAsync(userId, ct);
        return HandleResult(result);
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> GetMyTickets([FromQuery] TicketQueryParams queryParams, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await supportService.GetMyTicketsAsync(userId, queryParams, ct);
        return HandleResult(result);
    }

    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket([FromBody] CreateSupportTicketDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await supportService.CreateTicketAsync(userId, request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("tickets/{ticketId:guid}")]
    public async Task<IActionResult> GetTicketDetails(Guid ticketId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await supportService.GetTicketDetailsAsync(ticketId, userId, CurrentUser.HasRole("Admin"), ct);
        return HandleResult(result);
    }

    [HttpPost("tickets/{ticketId:guid}/messages")]
    public async Task<IActionResult> ReplyToTicket(Guid ticketId, [FromBody] ReplySupportTicketDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await supportService.ReplyToTicketAsync(ticketId, userId, CurrentUser.HasRole("Admin"), request, ct);
        return HandleResult(result);
    }
}
