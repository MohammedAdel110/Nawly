using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Domain.Events;
using Nawly.Persistence.Contexts;

namespace Nawly.Web.Workers;

public sealed class OutboxProcessorWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    private static readonly IReadOnlyDictionary<string, Type> DomainEventTypes =
        typeof(IDomainEvent).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IDomainEvent).IsAssignableFrom(t))
            .ToDictionary(t => t.Name, StringComparer.Ordinal);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessPendingOutboxMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OutboxProcessorWorker iteration.");
            }
        }
    }

    private async Task ProcessPendingOutboxMessagesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NawlyDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var now = clock.UtcNow;
        var graceCutoff = now.AddSeconds(-5);

        var messages = await dbContext.OutboxMessages
            .Where(m => !m.IsDeleted && m.ProcessedAt == null && m.OccurredAt <= graceCutoff && m.Attempts < 5)
            .OrderBy(m => m.OccurredAt)
            .Take(25)
            .ToListAsync(ct);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            try
            {
                if (!DomainEventTypes.TryGetValue(message.Type, out var eventType))
                {
                    message.Attempts += 1;
                    message.Error = $"Unknown domain event type '{message.Type}'.";
                    continue;
                }

                if (JsonSerializer.Deserialize(message.PayloadJson, eventType) is not IDomainEvent domainEvent)
                {
                    message.Attempts += 1;
                    message.Error = $"Could not deserialize payload for '{message.Type}'.";
                    continue;
                }

                await dispatcher.DispatchAsync([domainEvent], ct);

                if (message.ProcessedAt is null)
                {
                    message.ProcessedAt = clock.UtcNow;
                    message.Attempts += 1;
                    message.Error = null;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Failed processing OutboxMessage {MessageId} ({Type}).", message.Id, message.Type);
                message.Attempts += 1;
                message.Error = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
