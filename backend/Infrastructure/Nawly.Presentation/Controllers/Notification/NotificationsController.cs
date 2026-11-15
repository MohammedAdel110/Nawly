using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Shared.Common.Params.Notification;
using Nawly.Shared.DTOs.Notification;

namespace Nawly.Presentation.Controllers.Notification;

[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController(
    INotificationService notificationService,
    IConfiguration configuration) : ApiBaseController
{
    private const string DevFallbackVapidPublicKey =
        "BEl62iUYgUivxIkv69yViEuiBIa-Ib9-SkvMeAtA3LFgDzkrxZJjSgSnfckjBJuBkr3qBUYIHBQFLXYp5Nksh8";

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] NotificationQueryParams queryParams, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await notificationService.GetUserNotificationsAsync(userId, queryParams, ct);
        return HandleResult(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await notificationService.GetUnreadCountAsync(userId, ct);
        return HandleResult(result);
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await notificationService.MarkAsReadAsync(userId, notificationId, ct);
        return HandleResult(result);
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await notificationService.MarkAllAsReadAsync(userId, ct);
        return HandleResult(result);
    }

    [HttpPost("web-push/subscribe")]
    public async Task<IActionResult> SubscribeWebPush([FromBody] WebPushSubscriptionDto request, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await notificationService.SaveWebPushSubscriptionAsync(userId, request, ct);
        return HandleResult(result);
    }

    [HttpDelete("web-push/unsubscribe")]
    public async Task<IActionResult> UnsubscribeWebPush([FromQuery] string endpoint, CancellationToken ct)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return UnauthorizedProblem();

        var result = await notificationService.RemoveWebPushSubscriptionAsync(userId, endpoint, ct);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("web-push/vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        var configured = configuration["WebPush:PublicKey"];
        var publicKey = string.IsNullOrWhiteSpace(configured) ? DevFallbackVapidPublicKey : configured;
        return Ok(new { publicKey });
    }
}
