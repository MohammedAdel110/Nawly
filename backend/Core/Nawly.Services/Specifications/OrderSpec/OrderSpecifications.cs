using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Ordering;
using Nawly.Shared.Common.Params.Order;
using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Services.Specifications.OrderSpec;

public sealed class ActiveCartByCustomerSpecification : BaseSpecifications<Cart, Guid>
{
    public ActiveCartByCustomerSpecification(Guid customerProfileId, bool asNoTracking = false)
        : base(c => c.CustomerId == customerProfileId && !c.IsDeleted)
    {
        AddInclude(c => c.Store);
        AddInclude("Store.DeliveryZones.Zone");
        AddInclude("Store.OpeningHours");
        AddInclude("Items.Product.OptionGroups.Options");
        AddInclude("Items.Options.ProductOption");
        ApplySplitQuery();
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}

public sealed class OrdersFilterSpecification : BaseSpecifications<Order, Guid>
{
    public OrdersFilterSpecification(OrderQueryParams queryParams)
    {
        var status = queryParams.Status;
        var storeId = queryParams.StoreId;
        var customerId = queryParams.CustomerId;
        var fromDate = queryParams.FromDate;
        var toDate = queryParams.ToDate;
        var search = queryParams.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = o => !o.IsDeleted
            && (status == null || o.Status == status)
            && (storeId == null || o.StoreId == storeId)
            && (customerId == null || o.CustomerId == customerId)
            && (fromDate == null || o.PlacedAt >= fromDate)
            && (toDate == null || o.PlacedAt <= toDate)
            && (!hasSearch || o.OrderNumber.Contains(search!) || o.CustomerPhone.Contains(search!) || o.CustomerName.Contains(search!));

        AddInclude(o => o.Store);
        AddInclude(o => o.Items);
        AddOrderBy(o => o.PlacedAt, isDescending: true);
        ApplyPaging(queryParams.PageSize, queryParams.PageIndex);
        ApplyAsNoTracking();
        ApplySplitQuery();
    }
}

public sealed class OrdersCountSpecification : BaseSpecifications<Order, Guid>
{
    public OrdersCountSpecification(OrderQueryParams queryParams)
    {
        var status = queryParams.Status;
        var storeId = queryParams.StoreId;
        var customerId = queryParams.CustomerId;
        var fromDate = queryParams.FromDate;
        var toDate = queryParams.ToDate;
        var search = queryParams.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        Criteria = o => !o.IsDeleted
            && (status == null || o.Status == status)
            && (storeId == null || o.StoreId == storeId)
            && (customerId == null || o.CustomerId == customerId)
            && (fromDate == null || o.PlacedAt >= fromDate)
            && (toDate == null || o.PlacedAt <= toDate)
            && (!hasSearch || o.OrderNumber.Contains(search!) || o.CustomerPhone.Contains(search!) || o.CustomerName.Contains(search!));
    }
}

public sealed class OrderWithDetailsSpecification : BaseSpecifications<Order, Guid>
{
    public OrderWithDetailsSpecification(Guid orderId, bool asNoTracking = false)
        : base(o => o.Id == orderId && !o.IsDeleted)
    {
        AddInclude(o => o.Store);
        AddInclude("Items.Options");
        AddInclude(o => o.StatusHistory);
        ApplySplitQuery();
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}

public sealed class CustomerCompletedOrdersCountSpecification : BaseSpecifications<Order, Guid>
{
    public CustomerCompletedOrdersCountSpecification(Guid customerProfileId)
        : base(o => o.CustomerId == customerProfileId && !o.IsDeleted && o.Status == OrderStatus.Delivered)
    {
    }
}
