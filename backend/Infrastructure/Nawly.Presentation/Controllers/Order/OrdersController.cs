using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nawly.Abstraction.IServices.Order;
using Nawly.Abstraction.IServices.Review;
using Nawly.Shared.Common.Params.Order;
using Nawly.Shared.DTOs.Order;
using Nawly.Shared.DTOs.Review;

namespace Nawly.Presentation.Controllers.Order;

[Authorize]
[Route("api/v1/orders")]
public sealed class OrdersController(
    IOrderService orderService,
    IReviewService reviewService) : ApiBaseController
{
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequestDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        if (!TryResolveIdempotencyKey(request.IdempotencyKey, out var idempotencyKey, out var errorResult))
            return errorResult!;

        var result = await orderService.PlaceOrderAsync(userId, request, idempotencyKey, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryParams queryParams, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await orderService.GetCustomerOrdersAsync(userId, queryParams, ct);
        return HandleResult(result);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrderDetails(Guid orderId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await orderService.GetOrderDetailsAsync(
            orderId,
            userId,
            CurrentUser.Roles,
            CurrentUser.StoreId,
            ct);

        return HandleResult(result);
    }

    [HttpPost("{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid orderId, [FromBody] CancelOrderDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await orderService.CancelByCustomerAsync(userId, orderId, request, ct);
        return HandleResult(result);
    }

    [HttpPost("{orderId:guid}/review")]
    public async Task<IActionResult> CreateReview(Guid orderId, [FromBody] CreateReviewDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await reviewService.CreateReviewAsync(userId, orderId, request, ct);
        return HandleResult(result, StatusCodes.Status201Created);
    }
}
