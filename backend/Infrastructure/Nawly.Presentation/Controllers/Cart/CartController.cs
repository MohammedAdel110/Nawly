using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nawly.Abstraction.IServices.Cart;
using Nawly.Abstraction.IServices.Promotion;
using Nawly.Shared.DTOs.Cart;
using Nawly.Shared.DTOs.Promotion;

namespace Nawly.Presentation.Controllers.Cart;

[Authorize]
[Route("api/v1/cart")]
public sealed class CartController(
    ICartService cartService,
    IPromotionService promotionService) : ApiBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetActiveCart(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await cartService.GetActiveCartAsync(userId, ct);
        return HandleResult(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> UpsertItem([FromBody] UpsertCartItemDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await cartService.UpsertItemAsync(userId, request, ct);
        return HandleResult(result);
    }

    [HttpDelete("items/{cartItemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid cartItemId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await cartService.RemoveItemAsync(userId, cartItemId, ct);
        return HandleResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await cartService.ClearCartAsync(userId, ct);
        return HandleResult(result);
    }

    [HttpPost("quote")]
    public async Task<IActionResult> QuoteCart([FromBody] CartQuoteRequestDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await cartService.QuoteCartAsync(userId, request, ct);
        return HandleResult(result);
    }

    [HttpPost("coupon/validate")]
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequestDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await promotionService.ValidateCouponAsync(
            userId,
            request.StoreId,
            request.Subtotal,
            request.DeliveryFee,
            request.Code,
            ct);

        return HandleResult(result);
    }
}
