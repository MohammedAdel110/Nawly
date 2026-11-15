using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Ordering;
using Nawly.Shared.EnumHelper.DeliveryEnums;
using Nawly.Shared.EnumHelper.OrderEnums;
using Nawly.Shared.EnumHelper.StoreEnums;
using Nawly.Shared.EnumHelper.SupportEnums;

namespace Nawly.Services.Specifications.AdminSpec;

public sealed class PendingStoresCountSpecification : BaseSpecifications<Store, Guid>
{
    public PendingStoresCountSpecification()
        : base(s => !s.IsDeleted && s.Status == StoreStatus.Pending)
    {
    }
}

public sealed class PendingCouriersCountSpecification : BaseSpecifications<CourierProfile, Guid>
{
    public PendingCouriersCountSpecification()
        : base(c => !c.IsDeleted && c.Status == CourierStatus.Pending)
    {
    }
}

public sealed class OnlineCouriersCountSpecification : BaseSpecifications<CourierProfile, Guid>
{
    public OnlineCouriersCountSpecification()
        : base(c => !c.IsDeleted && c.Status == CourierStatus.Approved && c.IsOnline)
    {
    }
}

public sealed class ActiveOrdersCountSpecification : BaseSpecifications<Order, Guid>
{
    public ActiveOrdersCountSpecification()
        : base(o => !o.IsDeleted
            && o.Status != OrderStatus.Delivered
            && o.Status != OrderStatus.Cancelled
            && o.Status != OrderStatus.Rejected
            && o.Status != OrderStatus.Failed)
    {
    }
}

public sealed class DelayedOrdersCountSpecification : BaseSpecifications<Order, Guid>
{
    public DelayedOrdersCountSpecification(DateTimeOffset utcNow)
        : base(o => !o.IsDeleted
            && o.EstimatedDeliveryAt != null
            && o.EstimatedDeliveryAt < utcNow
            && o.Status != OrderStatus.Delivered
            && o.Status != OrderStatus.Cancelled
            && o.Status != OrderStatus.Rejected
            && o.Status != OrderStatus.Failed)
    {
    }
}

public sealed class OpenSupportTicketsCountSpecification : BaseSpecifications<SupportTicket, Guid>
{
    public OpenSupportTicketsCountSpecification()
        : base(t => !t.IsDeleted && (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress))
    {
    }
}

public sealed class BroadcastTargetUsersSpecification : BaseSpecifications<AppUser, Guid>
{
    public BroadcastTargetUsersSpecification()
        : base(u => u.Status == Nawly.Shared.EnumHelper.UserEnums.UserStatus.Active)
    {
        AddInclude(u => u.CourierProfile!);
        AddInclude(u => u.StoreStaffAssignments);
        AddInclude(u => u.CustomerProfile!);
        ApplyAsNoTracking();
        ApplySplitQuery();
    }
}
