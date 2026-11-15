using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nawly.Domain.Models.Common;
using Nawly.Domain.Models.Platform;

namespace Nawly.Persistence.Interceptors;

/// <summary>
/// Persists raised domain events into the transactional outbox table within the same database commit.
/// </summary>
public sealed class OutboxDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly HashSet<Guid> _recordedEventIds = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        PersistOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        PersistOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void PersistOutboxMessages(DbContext? context)
    {
        if (context is null)
            return;

        var aggregatesWithEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        if (aggregatesWithEvents.Count == 0)
            return;

        var outboxSet = context.Set<OutboxMessage>();

        foreach (var aggregate in aggregatesWithEvents)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                if (!_recordedEventIds.Add(domainEvent.EventId))
                    continue;

                if (outboxSet.Local.Any(m => m.Id == domainEvent.EventId))
                    continue;

                outboxSet.Add(new OutboxMessage
                {
                    Id = domainEvent.EventId,
                    Type = domainEvent.GetType().Name,
                    PayloadJson = JsonSerializer.Serialize<object>(domainEvent),
                    OccurredAt = domainEvent.OccurredAt,
                    CreatedAt = domainEvent.OccurredAt
                });
            }
        }
    }
}
