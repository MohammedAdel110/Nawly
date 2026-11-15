using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Geography;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Geography;
using Nawly.Services.Mapping.GeographyMapping;
using Nawly.Services.Specifications.StoreSpec;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Geography;

namespace Nawly.Services.ServicesImplementation.Geography;

public sealed class GeographyService(
    IUnitOfWork unitOfWork,
    IClock clock) : IGeographyService
{
    public async Task<Result<IReadOnlyList<DeliveryZoneDto>>> GetZonesAsync(
        bool activeOnly = true,
        CancellationToken ct = default)
    {
        var zones = await unitOfWork
            .GetRepository<DeliveryZone, Guid>()
            .GetAllWithSpecAsync(new DeliveryZonesListSpecification(activeOnly), ct);

        return Result.Success<IReadOnlyList<DeliveryZoneDto>>(zones.Select(z => z.ToDto()).ToList());
    }

    public async Task<Result<IReadOnlyList<AreaDto>>> GetAreasAsync(
        Guid? zoneId = null,
        bool activeOnly = true,
        CancellationToken ct = default)
    {
        var areas = await unitOfWork
            .GetRepository<Area, Guid>()
            .GetAllWithSpecAsync(new AreasListSpecification(zoneId, activeOnly), ct);

        return Result.Success<IReadOnlyList<AreaDto>>(areas.Select(a => a.ToDto()).ToList());
    }

    public async Task<Result<DeliveryZoneDto>> CreateZoneAsync(UpsertDeliveryZoneDto request, CancellationToken ct = default)
    {
        var zone = new DeliveryZone
        {
            Name = request.Name.Trim(),
            NameAr = request.NameAr.Trim(),
            BaseFee = request.BaseFee,
            PerKmFee = request.PerKmFee,
            MinOrderAmount = request.MinOrderAmount,
            FreeDeliveryThreshold = request.FreeDeliveryThreshold,
            EstimatedTravelMinutes = request.EstimatedTravelMinutes,
            IsActive = request.IsActive,
            PolygonGeoJson = request.PolygonGeoJson,
            CreatedAt = clock.UtcNow
        };

        await unitOfWork.GetRepository<DeliveryZone, Guid>().AddAsync(zone, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(zone.ToDto());
    }

    public async Task<Result<DeliveryZoneDto>> UpdateZoneAsync(
        Guid zoneId,
        UpsertDeliveryZoneDto request,
        CancellationToken ct = default)
    {
        var repo = unitOfWork.GetRepository<DeliveryZone, Guid>();
        var zone = await repo.GetByIdAsync(zoneId, ct);
        if (zone is null || zone.IsDeleted)
            return Result.Failure<DeliveryZoneDto>(Error.NotFound("geography.zone_not_found", "Delivery zone not found."));

        zone.Name = request.Name.Trim();
        zone.NameAr = request.NameAr.Trim();
        zone.BaseFee = request.BaseFee;
        zone.PerKmFee = request.PerKmFee;
        zone.MinOrderAmount = request.MinOrderAmount;
        zone.FreeDeliveryThreshold = request.FreeDeliveryThreshold;
        zone.EstimatedTravelMinutes = request.EstimatedTravelMinutes;
        zone.IsActive = request.IsActive;
        zone.PolygonGeoJson = request.PolygonGeoJson;
        zone.UpdatedAt = clock.UtcNow;

        repo.Update(zone);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(zone.ToDto());
    }

    public async Task<Result<AreaDto>> CreateAreaAsync(UpsertAreaDto request, CancellationToken ct = default)
    {
        var zone = await unitOfWork.GetRepository<DeliveryZone, Guid>().GetByIdAsync(request.ZoneId, ct);
        if (zone is null || zone.IsDeleted)
            return Result.Failure<AreaDto>(Error.NotFound("geography.zone_not_found", "Delivery zone not found."));

        var area = new Area
        {
            ZoneId = request.ZoneId,
            Name = request.Name.Trim(),
            NameAr = request.NameAr.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = clock.UtcNow,
            Zone = zone
        };

        await unitOfWork.GetRepository<Area, Guid>().AddAsync(area, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(area.ToDto());
    }

    public async Task<Result<AreaDto>> UpdateAreaAsync(Guid areaId, UpsertAreaDto request, CancellationToken ct = default)
    {
        var areaRepo = unitOfWork.GetRepository<Area, Guid>();
        var area = await areaRepo.GetByIdAsync(areaId, ct);
        if (area is null || area.IsDeleted)
            return Result.Failure<AreaDto>(Error.NotFound("geography.area_not_found", "Area not found."));

        var zone = await unitOfWork.GetRepository<DeliveryZone, Guid>().GetByIdAsync(request.ZoneId, ct);
        if (zone is null || zone.IsDeleted)
            return Result.Failure<AreaDto>(Error.NotFound("geography.zone_not_found", "Delivery zone not found."));

        area.ZoneId = request.ZoneId;
        area.Zone = zone;
        area.Name = request.Name.Trim();
        area.NameAr = request.NameAr.Trim();
        area.SortOrder = request.SortOrder;
        area.IsActive = request.IsActive;
        area.UpdatedAt = clock.UtcNow;

        areaRepo.Update(area);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(area.ToDto());
    }
}
