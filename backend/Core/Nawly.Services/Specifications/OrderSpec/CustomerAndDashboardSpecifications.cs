using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Geography;
using Nawly.Domain.Models.Identity;
using Nawly.Domain.Models.Ordering;
using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Services.Specifications.OrderSpec;

public sealed class CustomerProfileByUserIdSpecification : BaseSpecifications<CustomerProfile, Guid>
{
    public CustomerProfileByUserIdSpecification(Guid userId, bool asNoTracking = false)
        : base(c => c.UserId == userId && !c.IsDeleted)
    {
        AddInclude(c => c.User);
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}

public sealed class AddressesByCustomerSpecification : BaseSpecifications<Address, Guid>
{
    public AddressesByCustomerSpecification(Guid customerProfileId)
        : base(a => a.CustomerId == customerProfileId && !a.IsDeleted)
    {
        AddInclude(a => a.Area);
        AddInclude("Area.Zone");
        AddOrderBy(a => a.IsDefault, isDescending: true);
        AddOrderBy(a => a.CreatedAt, isDescending: true);
    }
}

public sealed class AddressByIdSpecification : BaseSpecifications<Address, Guid>
{
    public AddressByIdSpecification(Guid addressId, Guid customerProfileId, bool asNoTracking = false)
        : base(a => a.Id == addressId && a.CustomerId == customerProfileId && !a.IsDeleted)
    {
        AddInclude(a => a.Area);
        AddInclude("Area.Zone");
        if (asNoTracking)
            ApplyAsNoTracking();
    }
}

public sealed class CustomerOrdersCountSinceSpecification : BaseSpecifications<Order, Guid>
{
    public CustomerOrdersCountSinceSpecification(Guid? storeId, DateTimeOffset fromUtc)
        : base(o => !o.IsDeleted && o.PlacedAt >= fromUtc && (storeId == null || o.StoreId == storeId))
    {
        ApplyAsNoTracking();
    }
}

public sealed class PendingStoreOrdersSpecification : BaseSpecifications<Order, Guid>
{
    public PendingStoreOrdersSpecification(Guid storeId)
        : base(o => !o.IsDeleted && o.StoreId == storeId && (o.Status == OrderStatus.Placed || o.Status == OrderStatus.Accepted || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.ReadyForPickup || o.Status == OrderStatus.PickedUp))
    {
        ApplyAsNoTracking();
    }
}
