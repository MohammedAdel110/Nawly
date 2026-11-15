using Microsoft.EntityFrameworkCore;
using Nawly.Domain.Contracts.SpecificReposPattern;
using Nawly.Domain.Models.Ordering;
using Nawly.Persistence.Implementations.ReposPattern;
using Nawly.Shared.EnumHelper.OrderEnums;
using AppDbContext = Nawly.Persistence.Contexts.NawlyDbContext;

namespace Nawly.Persistence.Implementations.SpecificReposPattern;

public sealed class OrderRepository(AppDbContext dbContext)
    : GenericRepo<Order, Guid>(dbContext), IOrderRepository
{
    private static readonly TimeZoneInfo CairoTimeZone = ResolveCairoTimeZone();

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(o => o.Store)
            .Include(o => o.Items)
                .ThenInclude(i => i.Options)
            .Include(o => o.StatusHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public async Task<Order?> GetByIdempotencyKeyAsync(
        Guid customerId,
        Guid idempotencyKey,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(o => o.Store)
            .Include(o => o.Items)
                .ThenInclude(i => i.Options)
            .Include(o => o.StatusHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                o => o.CustomerId == customerId && o.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public async Task<int> GetNextDailySequenceAsync(DateOnly cairoDate, CancellationToken cancellationToken = default)
    {
        var isNpgsql = DbContext.Database.ProviderName?
            .Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        if (isNpgsql)
        {
            var settingKey = $"order.daily_seq.{cairoDate:yyyyMMdd}";
            var id = Guid.NewGuid();
            var description = $"Atomic daily order sequence for {cairoDate:yyyy-MM-dd}";
            var nowUtc = DateTimeOffset.UtcNow;

            var seq = await DbContext.Database
                .SqlQuery<int>($"""
                    INSERT INTO system_settings (id, key, value, type, description, created_at, is_deleted)
                    VALUES ({id}, {settingKey}, '1', 'int', {description}, {nowUtc}, false)
                    ON CONFLICT (key) DO UPDATE
                    SET value = ((system_settings.value)::integer + 1)::text,
                        updated_at = {nowUtc},
                        is_deleted = false
                    RETURNING (system_settings.value)::integer AS "Value"
                    """)
                .SingleAsync(cancellationToken);

            return seq;
        }

        // Non-relational / test fallback using Cairo calendar day boundaries in UTC.
        var startLocal = cairoDate.ToDateTime(TimeOnly.MinValue);
        var startUtc = new DateTimeOffset(startLocal, CairoTimeZone.GetUtcOffset(startLocal)).ToUniversalTime();
        var endUtc = startUtc.AddDays(1);

        var count = await DbSet
            .IgnoreQueryFilters()
            .CountAsync(o => o.PlacedAt >= startUtc && o.PlacedAt < endUtc, cancellationToken);

        return count + 1;
    }

    public async Task<IReadOnlyList<Order>> GetExpiredUnacceptedOrdersAsync(
        DateTimeOffset placedBeforeUtc,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(o => o.StatusHistory)
            .Where(o => o.Status == OrderStatus.Placed && o.PlacedAt < placedBeforeUtc)
            .OrderBy(o => o.PlacedAt)
            .ToListAsync(cancellationToken);

    private static TimeZoneInfo ResolveCairoTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }
    }
}
