using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Delivery;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Persistence.Contexts;
using Nawly.Shared.EnumHelper.DeliveryEnums;
using Nawly.Shared.EnumHelper.OrderEnums;

namespace Nawly.Web.Workers;

public sealed class DispatchEscalationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DispatchEscalationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan AlertCooldown = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _lastAlertedAt = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessPendingDispatchesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in DispatchEscalationWorker iteration.");
            }
        }
    }

    private async Task ProcessPendingDispatchesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NawlyDbContext>();
        var deliveryService = scope.ServiceProvider.GetRequiredService<IDeliveryService>();
        var realtimeNotifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var utcNow = clock.UtcNow;

        var pendingDeliveries = await dbContext.Deliveries
            .Include(d => d.Offers)
            .Include(d => d.Order)
            .Where(d => !d.IsDeleted
                && d.CourierId == null
                && (d.Status == DeliveryStatus.Unassigned || d.Status == DeliveryStatus.Offered)
                && !d.Order.IsDeleted
                && d.Order.Status != OrderStatus.Cancelled
                && d.Order.Status != OrderStatus.Rejected)
            .ToListAsync(ct);

        if (pendingDeliveries.Count == 0)
            return;

        var expiredNotifications = new List<(Guid CourierId, Guid OfferId)>();
        var anyExpiredModified = false;

        foreach (var delivery in pendingDeliveries)
        {
            foreach (var offer in delivery.Offers)
            {
                if (!offer.IsDeleted && offer.Response == DeliveryOfferResponse.Pending && offer.ExpiresAt <= utcNow)
                {
                    offer.Response = DeliveryOfferResponse.Expired;
                    offer.RespondedAt = utcNow;
                    anyExpiredModified = true;
                    expiredNotifications.Add((offer.CourierId, offer.Id));
                }
            }
        }

        if (anyExpiredModified)
            await dbContext.SaveChangesAsync(ct);

        foreach (var (courierId, offerId) in expiredNotifications)
            await realtimeNotifier.NotifyOfferExpiredOrTakenAsync(courierId, offerId, ct);

        foreach (var delivery in pendingDeliveries)
        {
            var hasValidPending = delivery.Offers.Any(o => !o.IsDeleted && o.Response == DeliveryOfferResponse.Pending && o.ExpiresAt > utcNow);
            if (hasValidPending)
                continue;

            var maxWave = delivery.Offers.Count > 0 ? delivery.Offers.Max(o => o.Wave) : 0;
            if (maxWave < 3)
            {
                if (maxWave == 0 && delivery.CreatedAt > utcNow.AddSeconds(-10))
                    continue;

                var nextWave = maxWave + 1;
                await deliveryService.DispatchWaveAsync(delivery.Id, nextWave, ct);
            }
            else if (!_lastAlertedAt.TryGetValue(delivery.Id, out var lastAlert) || utcNow - lastAlert >= AlertCooldown)
            {
                _lastAlertedAt[delivery.Id] = utcNow;
                await realtimeNotifier.NotifyDispatchAlertAsync(
                    delivery.OrderId,
                    "All 3 dispatch waves expired without courier acceptance; manual assignment recommended.",
                    ct);
            }
        }

        // Prune stale alert tracking entries no longer awaiting dispatch.
        var activeIds = pendingDeliveries.Select(d => d.Id).ToHashSet();
        foreach (var trackedId in _lastAlertedAt.Keys)
        {
            if (!activeIds.Contains(trackedId))
                _lastAlertedAt.TryRemove(trackedId, out _);
        }
    }
}
