using Nawly.Abstraction.IServices.Notification;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Shared.Common.Results;
using Nawly.Shared.EnumHelper.NotificationEnums;
using NotificationEntity = Nawly.Domain.Models.Engagement.Notification;

namespace Nawly.Services.ServicesImplementation.Notification.StrategyPattern;

public sealed class InAppNotificationStrategy(IUnitOfWork unitOfWork) : INotificationStrategy
{
    public NotificationChannelFlags Channel => NotificationChannelFlags.InApp;

    public async Task<Result> SendAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string? dataJson,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<NotificationEntity, Guid>();
        await repo.AddAsync(new NotificationEntity
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            DataJson = dataJson,
            Channels = NotificationChannelFlags.InApp
        }, ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
