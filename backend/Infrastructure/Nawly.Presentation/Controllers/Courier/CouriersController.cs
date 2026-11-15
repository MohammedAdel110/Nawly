using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nawly.Abstraction.IServices.Attachment;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Courier;
using Nawly.Abstraction.IServices.Delivery;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Attachment;
using Nawly.Shared.DTOs.Courier;
using Nawly.Shared.DTOs.Delivery;

namespace Nawly.Presentation.Controllers.Courier;

[Route("api/v1/courier")]
public sealed class CouriersController(
    ICourierService courierService,
    IDeliveryService deliveryService,
    IAttachmentService attachmentService,
    IClock clock) : ApiBaseController
{
    [AllowAnonymous]
    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyCourierDto request, CancellationToken ct)
    {
        var result = await courierService.ApplyAsync(request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [Authorize(Policy = "Courier")]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await courierService.GetProfileByUserIdAsync(userId, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpPost("online")]
    public async Task<IActionResult> ToggleOnline([FromBody] ToggleCourierOnlineDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await courierService.ToggleOnlineAsync(userId, request, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpPost("location")]
    public async Task<IActionResult> UpdateLocation([FromBody] CourierLocationUpdateDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await courierService.UpdateLocationAsync(userId, request, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpGet("earnings")]
    public async Task<IActionResult> GetEarnings(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var periodEnd = to ?? clock.UtcNow;
        var periodStart = from ?? periodEnd.AddDays(-30);

        var result = await courierService.GetEarningsAsync(userId, periodStart, periodEnd, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpGet("offers")]
    public async Task<IActionResult> GetPendingOffers(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await deliveryService.GetPendingOffersForCourierAsync(userId, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpPost("offers/{offerId:guid}/accept")]
    public async Task<IActionResult> AcceptOffer(Guid offerId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await deliveryService.AcceptOfferAsync(userId, offerId, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpPost("offers/{offerId:guid}/decline")]
    public async Task<IActionResult> DeclineOffer(Guid offerId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await deliveryService.DeclineOfferAsync(userId, offerId, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpGet("deliveries/active")]
    public async Task<IActionResult> GetActiveDelivery(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await deliveryService.GetActiveDeliveryForCourierAsync(userId, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpPost("deliveries/{deliveryId:guid}/arrived-store")]
    public async Task<IActionResult> MarkArrivedAtStore(Guid deliveryId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await deliveryService.MarkArrivedAtStoreAsync(userId, deliveryId, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpPost("deliveries/{deliveryId:guid}/picked-up")]
    public async Task<IActionResult> MarkPickedUp(Guid deliveryId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await deliveryService.MarkPickedUpAsync(userId, deliveryId, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpPost("deliveries/{deliveryId:guid}/arrived-customer")]
    public async Task<IActionResult> MarkArrivedAtCustomer(Guid deliveryId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await deliveryService.MarkArrivedAtCustomerAsync(userId, deliveryId, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpPost("deliveries/{deliveryId:guid}/delivered")]
    public async Task<IActionResult> MarkDelivered(
        Guid deliveryId,
        [FromBody] ConfirmDeliveryCodeDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await deliveryService.MarkDeliveredAsync(userId, deliveryId, request, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpPost("deliveries/{deliveryId:guid}/failed")]
    public async Task<IActionResult> MarkFailed(
        Guid deliveryId,
        [FromBody] FailDeliveryDto request,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await deliveryService.MarkFailedAsync(userId, deliveryId, request, ct);
        return HandleResult(result);
    }

    [Authorize(Policy = "Courier")]
    [HttpPost("media/upload")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadMedia(
        IFormFile? file,
        [FromForm] string? folder = "deliveries",
        CancellationToken ct = default)
    {
        if (!TryGetAuthenticatedUserId(out _))
            return UnauthorizedProblem();

        if (file is null || file.Length <= 0)
        {
            return HandleResult(Result.Failure<UploadedFileDto>(
                Error.Validation("attachment.file_required", "Image file is required.")));
        }

        await using var stream = file.OpenReadStream();
        var result = await attachmentService.UploadImageAsync(
            stream,
            file.FileName,
            file.ContentType ?? string.Empty,
            file.Length,
            string.IsNullOrWhiteSpace(folder) ? "deliveries" : folder,
            ct);

        return HandleResult(result, StatusCodes.Status201Created);
    }
}
