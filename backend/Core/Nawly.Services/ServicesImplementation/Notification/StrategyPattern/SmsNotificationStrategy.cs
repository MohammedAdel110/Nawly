using Nawly.Abstraction.IServices.Notification;
using Nawly.Abstraction.IServices.Sms;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Identity;
using Nawly.Shared.Common.Results;
using Nawly.Shared.EnumHelper.NotificationEnums;

namespace Nawly.Services.ServicesImplementation.Notification.StrategyPattern;

public sealed class SmsNotificationStrategy(
    IUnitOfWork unitOfWork,
    ISmsSender smsSender) : INotificationStrategy
{
    public NotificationChannelFlags Channel => NotificationChannelFlags.Sms;

    public async Task<Result> SendAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string? dataJson,
        CancellationToken ct = default)
    {
        var user = await unitOfWork.GetRepository<AppUser, Guid>().GetByIdAsync(userId, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.PhoneNumber))
            return Result.Failure(Error.NotFound("notification.user_phone_missing", "User phone number is unavailable."));

        var parameters = new Dictionary<string, string>
        {
            ["title"] = title,
            ["body"] = body
        };

        return await smsSender.SendAsync(user.PhoneNumber, type.ToString(), parameters, "notification", ct);
    }
}
