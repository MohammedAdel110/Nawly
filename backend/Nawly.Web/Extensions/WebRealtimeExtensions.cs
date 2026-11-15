using Microsoft.Extensions.DependencyInjection.Extensions;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Web.Hubs;
using Nawly.Web.Workers;

namespace Nawly.Web.Extensions;

public static class WebRealtimeExtensions
{
    public static IServiceCollection AddWebRealtimeAndWorkers(this IServiceCollection services)
    {
        services.AddSignalR();
        services.Replace(ServiceDescriptor.Singleton<IRealtimeNotifier, SignalRRealtimeNotifier>());

        services.AddHostedService<OutboxProcessorWorker>();
        services.AddHostedService<OrderTimeoutWorker>();
        services.AddHostedService<DispatchEscalationWorker>();

        return services;
    }

    public static WebApplication MapNawlyHubs(this WebApplication app)
    {
        app.MapHub<OrderTrackingHub>("/hubs/order-tracking");
        app.MapHub<DispatchHub>("/hubs/dispatch");
        app.MapHub<StoreOrdersHub>("/hubs/store-orders");

        return app;
    }
}
