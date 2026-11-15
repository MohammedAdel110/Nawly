using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Delivery;
using Nawly.Shared.Common.Params.Delivery;
using Nawly.Shared.EnumHelper.DeliveryEnums;

namespace Nawly.Services.Specifications.DeliverySpec;

public sealed class DeliveryWithDetailsSpecification : BaseSpecifications<Delivery, Guid>
{
    public DeliveryWithDetailsSpecification(Guid deliveryId, bool asNoTracking = false)
        : base(d => d.Id == deliveryId && !d.IsDeleted)
    {
        AddInclude("Order.Store");
        AddInclude("Courier.User");
        AddInclude(d => d.Offers);
        ApplySplitQuery();
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}

public sealed class PendingOffersForCourierSpecification : BaseSpecifications<DeliveryOffer, Guid>
{
    public PendingOffersForCourierSpecification(Guid courierProfileId, DateTimeOffset utcNow)
        : base(o => o.CourierId == courierProfileId
            && !o.IsDeleted
            && o.Response == DeliveryOfferResponse.Pending
            && o.ExpiresAt > utcNow)
    {
        AddInclude("Delivery.Order.Store");
        AddOrderBy(o => o.OfferedAt, isDescending: true);
        ApplyAsNoTracking();
        ApplySplitQuery();
    }
}

public sealed class ActiveDeliveryForCourierSpecification : BaseSpecifications<Delivery, Guid>
{
    public ActiveDeliveryForCourierSpecification(Guid courierProfileId, bool asNoTracking = true)
        : base(d => d.CourierId == courierProfileId
            && !d.IsDeleted
            && (d.Status == DeliveryStatus.Assigned
                || d.Status == DeliveryStatus.AtStore
                || d.Status == DeliveryStatus.PickedUp
                || d.Status == DeliveryStatus.AtCustomer))
    {
        AddInclude("Order.Store");
        AddInclude("Courier.User");
        AddOrderBy(d => d.AssignedAt!, isDescending: true);
        ApplySplitQuery();
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}

public sealed class DeliveriesFilterSpecification : BaseSpecifications<Delivery, Guid>
{
    public DeliveriesFilterSpecification(DeliveryQueryParams queryParams)
    {
        var status = queryParams.Status;
        var courierId = queryParams.CourierId;
        var search = queryParams.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = d => !d.IsDeleted
            && (status == null || d.Status == status)
            && (courierId == null || d.CourierId == courierId)
            && (!hasSearch || d.Order.OrderNumber.Contains(search!));

        AddInclude("Order.Store");
        AddInclude("Courier.User");
        AddOrderBy(d => d.CreatedAt, isDescending: true);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
        ApplySplitQuery();
    }
}
