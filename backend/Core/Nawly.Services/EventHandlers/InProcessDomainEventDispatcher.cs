using Microsoft.Extensions.DependencyInjection;
using Nawly.Domain.Events;

namespace Nawly.Services.EventHandlers;

public sealed class InProcessDomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
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
        }
    }
}
