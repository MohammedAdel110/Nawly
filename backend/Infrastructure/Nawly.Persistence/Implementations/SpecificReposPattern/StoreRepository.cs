using Microsoft.EntityFrameworkCore;
using Nawly.Domain.Contracts.SpecificReposPattern;
using Nawly.Domain.Models.Catalog;
using Nawly.Persistence.Implementations.ReposPattern;
using Nawly.Shared.EnumHelper.StoreEnums;
using AppDbContext = Nawly.Persistence.NawlyDbContext.NawlyDbContext;

namespace Nawly.Persistence.Implementations.SpecificReposPattern;

public sealed class StoreRepository(AppDbContext dbContext)
    : GenericRepo<Store, Guid>(dbContext), IStoreRepository
{
    public async Task<Store?> GetBySlugWithDetailsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        return await DbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Category)
            .Include(s => s.Area)
                .ThenInclude(a => a.Zone)
            .Include(s => s.OpeningHours)
            .Include(s => s.DeliveryZones)
                .ThenInclude(dz => dz.Zone)
            .Include(s => s.MenuCategories)
                .ThenInclude(mc => mc.Products)
                    .ThenInclude(p => p.OptionGroups)
                        .ThenInclude(g => g.Options)
            .FirstOrDefaultAsync(s => s.Slug == normalizedSlug, cancellationToken);
    }

    public async Task<IReadOnlyList<Store>> GetStoresDeliveringToAreaAsync(
        Guid areaId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Category)
            .Include(s => s.Area)
            .Include(s => s.OpeningHours)
            .Include(s => s.DeliveryZones)
                .ThenInclude(dz => dz.Zone)
            .Where(s => s.Status == StoreStatus.Active
                && (s.AreaId == areaId
                    || s.DeliveryZones.Any(dz =>
                        dz.Zone.IsActive
                        && dz.Zone.Areas.Any(a => a.Id == areaId && a.IsActive))))
            .OrderByDescending(s => s.RatingAvg)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);
}
