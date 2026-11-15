using Microsoft.Extensions.DependencyInjection;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Events;
using Nawly.Domain.Models.Platform;

namespace Nawly.Services.EventHandlers;

public sealed class InProcessDomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        var unitOfWork = serviceProvider.GetService<IUnitOfWork>();
        var outboxMarked = false;

        foreach (var domainEvent in events)
        {
            var eventType = domainEvent.GetType();
            var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = serviceProvider.GetServices(handlerInterface);

            foreach (var handler in handlers)
            {
                if (handler is null) continue;
                var method = handlerInterface.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
                if (method?.Invoke(handler, [domainEvent, cancellationToken]) is Task task)
                {
                    await task;
                }
            }

            if (unitOfWork is not null)
            {
                var outboxRepo = unitOfWork.GetRepository<OutboxMessage, Guid>();
                var outbox = await outboxRepo.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (outbox is not null && outbox.ProcessedAt is null)
                {
                    outbox.ProcessedAt = DateTimeOffset.UtcNow;
                    outbox.Attempts += 1;
                    outbox.Error = null;
                    outboxRepo.Update(outbox);
                    outboxMarked = true;
                }
            }
        }

        if (outboxMarked && unitOfWork is not null)
            await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
