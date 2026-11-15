using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nawly.Abstraction.IServices.Catalog;
using Nawly.Abstraction.IServices.Geography;
using Nawly.Abstraction.IServices.Review;
using Nawly.Abstraction.IServices.Store;
using Nawly.Shared.Common.Params.Catalog;
using Nawly.Shared.Common.Params.Review;
using Nawly.Shared.Common.Params.Store;
using Nawly.Shared.DTOs.Store;

namespace Nawly.Presentation.Controllers.Discovery;

[Route("api/v1/discovery")]
public sealed class DiscoveryController(
    IGeographyService geographyService,
    IStoreService storeService,
    ICatalogService catalogService,
    IReviewService reviewService) : ApiBaseController
{
    [AllowAnonymous]
    [HttpGet("zones")]
    public async Task<IActionResult> GetZones(CancellationToken ct)
    {
        var result = await geographyService.GetZonesAsync(activeOnly: true, ct);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("areas")]
    public async Task<IActionResult> GetAreas([FromQuery] Guid? zoneId, CancellationToken ct)
    {
        var result = await geographyService.GetAreasAsync(zoneId, activeOnly: true, ct);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await storeService.GetCategoriesAsync(activeOnly: true, ct);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("stores")]
    public async Task<IActionResult> SearchStores([FromQuery] StoreQueryParams queryParams, CancellationToken ct)
    {
        var result = await storeService.SearchStoresAsync(queryParams, ct);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("stores/{slug}")]
    public async Task<IActionResult> GetStoreBySlug(string slug, CancellationToken ct)
    {
        var result = await storeService.GetStoreBySlugAsync(slug, ct);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("stores/by-id/{storeId:guid}")]
    public async Task<IActionResult> GetStoreById(Guid storeId, CancellationToken ct)
    {
        var result = await storeService.GetStoreByIdAsync(storeId, ct);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("stores/{storeId:guid}/menu")]
    public async Task<IActionResult> GetStoreMenu(Guid storeId, CancellationToken ct)
    {
        var result = await catalogService.GetStoreMenuAsync(storeId, includeUnavailable: false, ct);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("stores/{storeId:guid}/reviews")]
    public async Task<IActionResult> GetStoreReviews(Guid storeId, [FromQuery] ReviewQueryParams queryParams, CancellationToken ct)
    {
        queryParams.StoreId = storeId;
        var result = await reviewService.GetReviewsAsync(queryParams, ct);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts([FromQuery] ProductQueryParams queryParams, CancellationToken ct)
    {
        var result = await catalogService.SearchProductsAsync(queryParams, ct);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpPost("stores/apply")]
    public async Task<IActionResult> ApplyStore([FromBody] ApplyStoreDto request, CancellationToken ct)
    {
        var result = await storeService.ApplyAsync(request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [Authorize]
    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await storeService.GetFavoritesAsync(userId, ct);
        return HandleResult(result);
    }

    [Authorize]
    [HttpPost("favorites/{storeId:guid}")]
    public async Task<IActionResult> AddFavorite(Guid storeId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await storeService.AddFavoriteAsync(userId, storeId, ct);
        return HandleResult(result);
    }

    [Authorize]
    [HttpDelete("favorites/{storeId:guid}")]
    public async Task<IActionResult> RemoveFavorite(Guid storeId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await storeService.RemoveFavoriteAsync(userId, storeId, ct);
        return HandleResult(result);
    }
}
