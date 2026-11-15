using Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;
using Nawly.Domain.Models.Catalog;
using Nawly.Domain.Models.Engagement;
using Nawly.Domain.Models.Geography;

namespace Nawly.Services.Specifications.StoreSpec;

public sealed class DeliveryZonesListSpecification : BaseSpecifications<DeliveryZone, Guid>
{
    public DeliveryZonesListSpecification(bool activeOnly)
        : base(z => !z.IsDeleted && (!activeOnly || z.IsActive))
    {
        AddOrderBy(z => z.Name);
        ApplyAsNoTracking();
    }
}

public sealed class AreasListSpecification : BaseSpecifications<Area, Guid>
{
    public AreasListSpecification(Guid? zoneId, bool activeOnly)
        : base(a => !a.IsDeleted && (zoneId == null || a.ZoneId == zoneId) && (!activeOnly || a.IsActive))
    {
        AddInclude(a => a.Zone);
        AddOrderBy(a => a.SortOrder);
        AddOrderBy(a => a.Name);
        ApplyAsNoTracking();
    }
}

public sealed class AreaWithZoneByIdSpecification : BaseSpecifications<Area, Guid>
{
    public AreaWithZoneByIdSpecification(Guid areaId)
        : base(a => a.Id == areaId && !a.IsDeleted)
    {
        AddInclude(a => a.Zone);
    }
}

public sealed class StoreStaffByUserSpecification : BaseSpecifications<StoreStaff, Guid>
{
    public StoreStaffByUserSpecification(Guid storeId, Guid userId)
        : base(s => s.StoreId == storeId && s.UserId == userId && !s.IsDeleted)
    {
        AddInclude(s => s.User);
    }
}

public sealed class CustomerFavoriteByPairSpecification : BaseSpecifications<Favorite, Guid>
{
    public CustomerFavoriteByPairSpecification(Guid customerProfileId, Guid storeId)
        : base(f => f.CustomerId == customerProfileId && f.StoreId == storeId && !f.IsDeleted)
    {
    }
}
