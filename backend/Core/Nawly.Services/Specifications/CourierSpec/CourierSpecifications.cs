using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Delivery;
using Nawly.Domain.Models.Identity;
using Nawly.Shared.Common.Params.Courier;
using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Services.Specifications.CourierSpec;

public sealed class CourierByUserIdSpecification : BaseSpecifications<CourierProfile, Guid>
{
    public CourierByUserIdSpecification(Guid userId, bool asNoTracking = false)
        : base(c => c.UserId == userId && !c.IsDeleted)
    {
        AddInclude(c => c.User);
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}

public sealed class CouriersFilterSpecification : BaseSpecifications<CourierProfile, Guid>
{
    public CouriersFilterSpecification(CourierQueryParams queryParams)
    {
        var status = queryParams.Status;
        var isOnline = queryParams.IsOnline;
        var search = queryParams.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = c => !c.IsDeleted
            && (status == null || c.Status == status)
            && (isOnline == null || c.IsOnline == isOnline)
            && (!hasSearch || c.User.FullName.Contains(search!) || (c.User.PhoneNumber != null && c.User.PhoneNumber.Contains(search!)));

        AddInclude(c => c.User);
        AddOrderBy(c => c.CreatedAt, isDescending: true);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
    }
}

public sealed class CouriersCountSpecification : BaseSpecifications<CourierProfile, Guid>
{
    public CouriersCountSpecification(CourierQueryParams queryParams)
    {
        var status = queryParams.Status;
        var isOnline = queryParams.IsOnline;
        var search = queryParams.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = c => !c.IsDeleted
            && (status == null || c.Status == status)
            && (isOnline == null || c.IsOnline == isOnline)
            && (!hasSearch || c.User.FullName.Contains(search!) || (c.User.PhoneNumber != null && c.User.PhoneNumber.Contains(search!)));
    }
}

public sealed class CourierCompletedDeliveriesSpecification : BaseSpecifications<Delivery, Guid>
{
    public CourierCompletedDeliveriesSpecification(Guid courierProfileId, DateTimeOffset periodStart, DateTimeOffset periodEnd)
        : base(d => d.CourierId == courierProfileId
            && !d.IsDeleted
            && d.Status == DeliveryStatus.Delivered
            && d.DeliveredAt >= periodStart
            && d.DeliveredAt <= periodEnd)
    {
        AddOrderBy(d => d.DeliveredAt!, isDescending: true);
        ApplyAsNoTracking();
    }
}
