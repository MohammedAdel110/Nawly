using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Engagement;
using Nawly.Shared.Common.Params.Notification;

namespace Nawly.Services.Specifications.NotificationSpec;

public sealed class UserNotificationsSpecification : BaseSpecifications<Notification, Guid>
{
    public UserNotificationsSpecification(Guid userId, NotificationQueryParams queryParams)
    {
        var unreadOnly = queryParams.UnreadOnly;

        Criteria = n => !n.IsDeleted
            && n.UserId == userId
            && (unreadOnly != true || n.ReadAt == null);

        AddOrderBy(n => n.CreatedAt, isDescending: true);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
    }
}

public sealed class UserNotificationsCountSpecification : BaseSpecifications<Notification, Guid>
{
    public UserNotificationsCountSpecification(Guid userId, bool unreadOnly)
        : base(n => !n.IsDeleted && n.UserId == userId && (!unreadOnly || n.ReadAt == null))
    {
    }
}

public sealed class UnreadNotificationsSpecification : BaseSpecifications<Notification, Guid>
{
    public UnreadNotificationsSpecification(Guid userId)
        : base(n => !n.IsDeleted && n.UserId == userId && n.ReadAt == null)
    {
    }
}

public sealed class UserWebPushSubscriptionsSpecification : BaseSpecifications<WebPushSubscription, Guid>
{
    public UserWebPushSubscriptionsSpecification(Guid userId, string? endpoint = null)
        : base(s => !s.IsDeleted && s.UserId == userId && (endpoint == null || s.Endpoint == endpoint))
    {
    }
}
