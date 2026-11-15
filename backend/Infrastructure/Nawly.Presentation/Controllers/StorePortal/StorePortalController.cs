using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nawly.Abstraction.IServices.Attachment;
using Nawly.Abstraction.IServices.Catalog;
using Nawly.Abstraction.IServices.Review;
using Nawly.Abstraction.IServices.Store;
using Nawly.Presentation.Extensions;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Attachment;
using Nawly.Shared.DTOs.Catalog;
using Nawly.Shared.DTOs.Review;
using Nawly.Shared.DTOs.Store;

namespace Nawly.Presentation.Controllers.StorePortal;

[Authorize(Policy = "VendorAccess")]
[Route("api/v1/store-portal")]
public sealed class StorePortalController(
    IStoreService storeService,
    ICatalogService catalogService,
    IReviewService reviewService,
    IAttachmentService attachmentService) : ApiBaseController
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] Guid? storeId, CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await storeService.GetDashboardAsync(resolvedStoreId, ct);
        return HandleResult(result);
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings([FromQuery] Guid? storeId, CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await storeService.GetStoreByIdAsync(resolvedStoreId, ct);
        return HandleResult(result);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateStoreSettingsDto request,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await storeService.UpdateSettingsAsync(resolvedStoreId, userId, request, ct);
        return HandleResult(result);
    }

    [HttpPut("opening-hours")]
    public async Task<IActionResult> UpdateOpeningHours(
        [FromBody] IReadOnlyList<StoreOpeningHourDto> hours,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await storeService.UpdateOpeningHoursAsync(resolvedStoreId, userId, hours, ct);
        return HandleResult(result);
    }

    [HttpPost("pause")]
    public async Task<IActionResult> PauseStore(
        [FromBody] PauseStoreDto request,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await storeService.PauseStoreAsync(resolvedStoreId, userId, request, ct);
        return HandleResult(result);
    }

    [HttpPost("resume")]
    public async Task<IActionResult> ResumeStore([FromQuery] Guid? storeId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await storeService.ResumeStoreAsync(resolvedStoreId, userId, ct);
        return HandleResult(result);
    }

    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff([FromQuery] Guid? storeId, CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await storeService.GetStaffAsync(resolvedStoreId, ct);
        return HandleResult(result);
    }

    [HttpPost("staff")]
    public async Task<IActionResult> AddStaff(
        [FromBody] AddStoreStaffDto request,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await storeService.AddStaffAsync(resolvedStoreId, userId, request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpDelete("staff/{staffUserId:guid}")]
    public async Task<IActionResult> RemoveStaff(Guid staffUserId, [FromQuery] Guid? storeId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await storeService.RemoveStaffAsync(resolvedStoreId, userId, staffUserId, ct);
        return HandleResult(result);
    }

    [HttpGet("menu")]
    public async Task<IActionResult> GetMenu([FromQuery] Guid? storeId, CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await catalogService.GetStoreMenuAsync(resolvedStoreId, includeUnavailable: true, ct);
        return HandleResult(result);
    }

    [HttpPost("menu/categories")]
    public async Task<IActionResult> CreateMenuCategory(
        [FromBody] UpsertMenuCategoryDto request,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await catalogService.CreateMenuCategoryAsync(resolvedStoreId, request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("menu/categories/{categoryId:guid}")]
    public async Task<IActionResult> UpdateMenuCategory(
        Guid categoryId,
        [FromBody] UpsertMenuCategoryDto request,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await catalogService.UpdateMenuCategoryAsync(resolvedStoreId, categoryId, request, ct);
        return HandleResult(result);
    }

    [HttpDelete("menu/categories/{categoryId:guid}")]
    public async Task<IActionResult> DeleteMenuCategory(Guid categoryId, [FromQuery] Guid? storeId, CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await catalogService.DeleteMenuCategoryAsync(resolvedStoreId, categoryId, ct);
        return HandleResult(result);
    }

    [HttpPost("menu/products")]
    public async Task<IActionResult> CreateProduct(
        [FromBody] UpsertProductDto request,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await catalogService.CreateProductAsync(resolvedStoreId, request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("menu/products/{productId:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid productId,
        [FromBody] UpsertProductDto request,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await catalogService.UpdateProductAsync(resolvedStoreId, productId, request, ct);
        return HandleResult(result);
    }

    [HttpDelete("menu/products/{productId:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid productId, [FromQuery] Guid? storeId, CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await catalogService.DeleteProductAsync(resolvedStoreId, productId, ct);
        return HandleResult(result);
    }

    [HttpPatch("menu/products/{productId:guid}/availability")]
    public async Task<IActionResult> ToggleProductAvailability(
        Guid productId,
        [FromBody] ToggleProductAvailabilityDto request,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await catalogService.ToggleProductAvailabilityAsync(resolvedStoreId, productId, request, ct);
        return HandleResult(result);
    }

    [HttpPost("menu/products/{productId:guid}/option-groups")]
    public async Task<IActionResult> CreateOptionGroup(
        Guid productId,
        [FromBody] UpsertProductOptionGroupDto request,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await catalogService.UpsertOptionGroupAsync(resolvedStoreId, productId, null, request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("menu/products/{productId:guid}/option-groups/{groupId:guid}")]
    public async Task<IActionResult> UpdateOptionGroup(
        Guid productId,
        Guid groupId,
        [FromBody] UpsertProductOptionGroupDto request,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await catalogService.UpsertOptionGroupAsync(resolvedStoreId, productId, groupId, request, ct);
        return HandleResult(result);
    }

    [HttpDelete("menu/products/{productId:guid}/option-groups/{groupId:guid}")]
    public async Task<IActionResult> DeleteOptionGroup(
        Guid productId,
        Guid groupId,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await catalogService.DeleteOptionGroupAsync(resolvedStoreId, productId, groupId, ct);
        return HandleResult(result);
    }

    [HttpPost("reviews/{reviewId:guid}/reply")]
    public async Task<IActionResult> ReplyToReview(
        Guid reviewId,
        [FromBody] ReplyReviewDto request,
        [FromQuery] Guid? storeId,
        CancellationToken ct)
    {
        if (!TryResolveStoreId(storeId, out var resolvedStoreId, out var errorResult))
            return errorResult!;

        var result = await reviewService.ReplyToReviewAsync(resolvedStoreId, reviewId, request, ct);
        return HandleResult(result);
    }

    [HttpPost("media/upload")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadMedia(
        IFormFile? file,
        [FromForm] string? folder = "catalog",
        [FromQuery] Guid? storeId = null,
        CancellationToken ct = default)
    {
        if (!TryResolveStoreId(storeId, out _, out var errorResult))
            return errorResult!;

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
            string.IsNullOrWhiteSpace(folder) ? "catalog" : folder,
            ct);

        return HandleResult(result, StatusCodes.Status201Created);
    }

    private bool TryResolveStoreId(Guid? explicitStoreId, out Guid resolvedStoreId, out IActionResult? errorResult)
    {
        if (CurrentUser.HasRole("Admin"))
        {
            var candidate = explicitStoreId ?? CurrentUser.StoreId;
            if (candidate is { } adminStoreId && adminStoreId != Guid.Empty)
            {
                resolvedStoreId = adminStoreId;
                errorResult = null;
                return true;
            }

            resolvedStoreId = Guid.Empty;
            errorResult = Result.Failure(Error.Forbidden(
                "store.context_missing",
                "Specify a valid storeId query parameter for admin store portal access."))
                .ToActionResult(this);
            return false;
        }

        if (CurrentUser.StoreId is not { } tokenStoreId || tokenStoreId == Guid.Empty)
        {
            resolvedStoreId = Guid.Empty;
            errorResult = Result.Failure(Error.Forbidden(
                "store.context_missing",
                "Authenticated vendor account is not linked to an active store."))
                .ToActionResult(this);
            return false;
        }

        if (explicitStoreId.HasValue && explicitStoreId.Value != tokenStoreId)
        {
            resolvedStoreId = Guid.Empty;
            errorResult = Result.Failure(Error.Forbidden(
                "store.forbidden_store_scope",
                "You do not have permission to access another store's portal."))
                .ToActionResult(this);
            return false;
        }

        resolvedStoreId = tokenStoreId;
        errorResult = null;
        return true;
    }
}
