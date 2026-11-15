using Microsoft.Extensions.Logging;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Engagement;
using Nawly.Services.Specifications.NotificationSpec;
using Nawly.Shared.Common.Results;
using Nawly.Shared.EnumHelper.NotificationEnums;

namespace Nawly.Services.ServicesImplementation.Notification.StrategyPattern;

/// <summary>
/// Fake/Development Web Push strategy that inspects registered user push subscriptions and
/// logs the push payload instead of invoking an external VAPID push service.
/// </summary>
public sealed class WebPushNotificationStrategy(
    IUnitOfWork unitOfWork,
    ILogger<WebPushNotificationStrategy> logger) : INotificationStrategy
{
    public NotificationChannelFlags Channel => NotificationChannelFlags.WebPush;

    public async Task<Result> SendAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string? dataJson,
        CancellationToken ct = default)
    {
        var subscriptions = await unitOfWork
            .GetRepository<WebPushSubscription, Guid>()
            .GetAllWithSpecAsync(new UserWebPushSubscriptionsSpecification(userId), ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[Dev Fake WebPush] Type={Type}, User={UserId}, Subscriptions={Count}, Title='{Title}'",
                type,
                userId,
                subscriptions.Count,
                title);
        }

        return Result.Success();
    }
}
