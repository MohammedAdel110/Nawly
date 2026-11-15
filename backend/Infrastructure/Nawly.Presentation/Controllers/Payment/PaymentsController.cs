using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nawly.Abstraction.IServices.Order;
using Nawly.Abstraction.IServices.Payment;

namespace Nawly.Presentation.Controllers.Payment;

[Authorize]
[Route("api/v1/payments")]
public sealed class PaymentsController(
    IPaymentService paymentService,
    IOrderService orderService) : ApiBaseController
{
    [HttpPost("orders/{orderId:guid}/initiate")]
    public async Task<IActionResult> InitiatePayment(Guid orderId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        if (!TryResolveIdempotencyKey(null, out var idempotencyKey, out var errorResult, "payment.invalid_idempotency_key"))
            return errorResult!;

        var result = await paymentService.InitiateAsync(userId, orderId, idempotencyKey, ct);
        return HandleResult(result);
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> GetByOrderId(Guid orderId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var orderAccess = await orderService.GetOrderDetailsAsync(
            orderId,
            userId,
            CurrentUser.Roles,
            CurrentUser.StoreId,
            ct);

        if (orderAccess.IsFailure)
            return HandleResult(orderAccess);

        var result = await paymentService.GetByOrderIdAsync(orderId, ct);
        return HandleResult(result);
    }
}
