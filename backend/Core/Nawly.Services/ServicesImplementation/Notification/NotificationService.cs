using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Identity;
using Nawly.Services.Mapping.NotificationMapping;
using Nawly.Services.Specifications.AdminSpec;
using Nawly.Services.Specifications.NotificationSpec;
using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Notification;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Notification;
using Nawly.Shared.EnumHelper.NotificationEnums;
using Nawly.Shared.EnumHelper.UserEnums;
using NotificationEntity = Nawly.Domain.Models.Engagement.Notification;

namespace Nawly.Services.ServicesImplementation.Notification;

public sealed class NotificationService(
    IUnitOfWork unitOfWork,
    IEnumerable<INotificationStrategy> strategies,
    IClock clock) : INotificationService
{
    public async Task<Result<PaginationResponse<NotificationDto>>> GetUserNotificationsAsync(
        Guid userId,
        NotificationQueryParams queryParams,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<NotificationEntity, Guid>();
        var items = await repo.GetAllWithSpecAsync(new UserNotificationsSpecification(userId, queryParams), ct);
        var count = await repo.GetCountAsync(new UserNotificationsCountSpecification(userId, queryParams.UnreadOnly == true), ct);

        return Result.Success(new PaginationResponse<NotificationDto>(
            queryParams.PageIndex,
            queryParams.PageSize,
            count,
            items.Select(n => n.ToDto()).ToList()));
    }

    public async Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        var count = await unitOfWork
            .GetRepository<NotificationEntity, Guid>()
            .GetCountAsync(new UserNotificationsCountSpecification(userId, unreadOnly: true), ct);

        return Result.Success(count);
    }

    public async Task<Result> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<NotificationEntity, Guid>();
        var notification = await repo.GetByIdAsync(notificationId, ct);
        if (notification is null || notification.IsDeleted || notification.UserId != userId)
            return Result.Failure(Error.NotFound("notification.not_found", "Notification not found."));

        notification.MarkAsRead(clock.UtcNow);
        repo.Update(notification);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<NotificationEntity, Guid>();
        var unread = await repo.GetAllWithSpecAsync(new UnreadNotificationsSpecification(userId), ct);
        if (unread.Count == 0)
            return Result.Success();

        foreach (var item in unread)
        {
            item.MarkAsRead(clock.UtcNow);
            repo.Update(item);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SaveWebPushSubscriptionAsync(
        Guid userId,
        WebPushSubscriptionDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<WebPushSubscription, Guid>();
        var existing = await repo.GetByIdWithSpecAsync(new UserWebPushSubscriptionsSpecification(userId, request.Endpoint), ct);

        if (existing is null)
        {
            await repo.AddAsync(new WebPushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                UserAgent = request.UserAgent,
                LastUsedAt = clock.UtcNow,
                CreatedAt = clock.UtcNow
            }, ct);
        }
        else
        {
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            existing.UserAgent = request.UserAgent;
            existing.LastUsedAt = clock.UtcNow;
            repo.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveWebPushSubscriptionAsync(Guid userId, string endpoint, CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<WebPushSubscription, Guid>();
        var existing = await repo.GetByIdWithSpecAsync(new UserWebPushSubscriptionsSpecification(userId, endpoint), ct);
        if (existing is null)
            return Result.Success();

        existing.IsDeleted = true;
        existing.DeletedAt = clock.UtcNow;
        repo.Update(existing);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SendToUserAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        NotificationChannelFlags channels,
        string? dataJson = null,
        CancellationToken ct = default)
    {
        var executed = false;
        foreach (var strategy in strategies)
        {
            if (channels.HasFlag(strategy.Channel))
            {
                await strategy.SendAsync(userId, type, title, body, dataJson, ct);
                executed = true;
            }
        }

        if (!executed && channels.HasFlag(NotificationChannelFlags.InApp))
        {
            var repo = unitOfWork.GetRepository<NotificationEntity, Guid>();
            await repo.AddAsync(new NotificationEntity
            {
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                DataJson = dataJson,
                Channels = NotificationChannelFlags.InApp,
                CreatedAt = clock.UtcNow
            }, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    public async Task<Result<int>> BroadcastAsync(
        Guid adminUserId,
        BroadcastNotificationDto request,
        CancellationToken ct = default)
    {
        var users = await unitOfWork
            .GetRepository<AppUser, Guid>()
            .GetAllWithSpecAsync(new BroadcastTargetUsersSpecification(), ct);

        var targets = users.Where(u => request.TargetRole switch
        {
            UserRole.Courier => u.CourierProfile is not null,
            UserRole.StoreOwner => u.StoreStaffAssignments.Any(s => s.IsActive),
            UserRole.Customer => u.CustomerProfile is not null,
            null => true,
            _ => true
        }).ToList();

        var sentCount = 0;
        foreach (var user in targets)
        {
            await SendToUserAsync(
                user.Id,
                NotificationType.SystemBroadcast,
                request.Title.Trim(),
                request.Body.Trim(),
                request.Channels,
                request.DataJson,
                ct);
            sentCount++;
        }

        _ = adminUserId;
        return Result.Success(sentCount);
    }
}
