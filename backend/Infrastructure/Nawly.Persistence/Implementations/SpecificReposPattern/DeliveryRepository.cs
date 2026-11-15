using Microsoft.EntityFrameworkCore;
using Nawly.Domain.Contracts.SpecificReposPattern;
using Nawly.Domain.Models.Delivery;
using Nawly.Domain.Models.Identity;
using Nawly.Persistence.Implementations.ReposPattern;
using Nawly.Shared.EnumHelper.DeliveryEnums;
using AppDbContext = Nawly.Persistence.Contexts.NawlyDbContext;

namespace Nawly.Persistence.Implementations.SpecificReposPattern;

public sealed class DeliveryRepository(AppDbContext dbContext)
    : GenericRepo<Delivery, Guid>(dbContext), IDeliveryRepository
{
    public async Task<bool> TryAssignCourierAtomicallyAsync(
        Guid deliveryId,
        Guid courierId,
        DateTimeOffset assignedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (DbContext.Database.IsRelational())
        {
            var affectedRows = await DbSet
                .Where(d => d.Id == deliveryId
                    && d.CourierId == null
                    && (d.Status == DeliveryStatus.Unassigned || d.Status == DeliveryStatus.Offered))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(d => d.CourierId, courierId)
                        .SetProperty(d => d.Status, DeliveryStatus.Assigned)
                        .SetProperty(d => d.AssignedAt, assignedAtUtc)
                        .SetProperty(d => d.UpdatedAt, assignedAtUtc),
                    cancellationToken);

            if (affectedRows == 0)
                return false;

            // Detach any already-tracked instance so the caller reloads fresh state & updated xmin token.
            var tracked = DbContext.ChangeTracker
                .Entries<Delivery>()
                .FirstOrDefault(e => e.Entity.Id == deliveryId);
            if (tracked is not null)
                tracked.State = EntityState.Detached;

            return true;
        }

        // Non-relational fallback for in-memory test providers.
        var delivery = await DbSet.FirstOrDefaultAsync(
            d => d.Id == deliveryId
                && d.CourierId == null
                && (d.Status == DeliveryStatus.Unassigned || d.Status == DeliveryStatus.Offered),
            cancellationToken);

        if (delivery is null)
            return false;

        var assignResult = delivery.AssignCourier(courierId, assignedAtUtc);
        return assignResult.IsSuccess;
    }

    public async Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(d => d.Courier)
                .ThenInclude(c => c!.User)
            .Include(d => d.Offers)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.OrderId == orderId, cancellationToken);

    public async Task<IReadOnlyList<Delivery>> GetActiveByCourierIdAsync(
        Guid courierId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(d => d.Order)
                .ThenInclude(o => o.Store)
            .Where(d => d.CourierId == courierId
                && (d.Status == DeliveryStatus.Assigned
                    || d.Status == DeliveryStatus.AtStore
                    || d.Status == DeliveryStatus.PickedUp
                    || d.Status == DeliveryStatus.AtCustomer))
            .OrderBy(d => d.AssignedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CourierProfile>> GetEligibleCouriersForDispatchAsync(
        DateTimeOffset locationUpdatedSinceUtc,
        CancellationToken cancellationToken = default)
        => await DbContext.CourierProfiles
            .Include(c => c.User)
            .Where(c => c.IsOnline
                && c.Status == CourierStatus.Approved
                && c.LastLocationAt != null
                && c.LastLocationAt >= locationUpdatedSinceUtc
                && !DbSet.Any(d => d.CourierId == c.Id
                    && (d.Status == DeliveryStatus.Assigned
                        || d.Status == DeliveryStatus.AtStore
                        || d.Status == DeliveryStatus.PickedUp
                        || d.Status == DeliveryStatus.AtCustomer)))
            .OrderByDescending(c => c.LastLocationAt)
            .ToListAsync(cancellationToken);
}
