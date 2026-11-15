using Nawly.Abstraction.IServices.Order;

namespace Nawly.Web.Workers;

public sealed class OrderTimeoutWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderTimeoutWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AcceptanceTimeout = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var rejected = await orderService.AutoRejectExpiredOrdersAsync(AcceptanceTimeout, stoppingToken);

                if (rejected > 0)
                    logger.LogInformation("OrderTimeoutWorker auto-rejected {Count} expired pending order(s).", rejected);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OrderTimeoutWorker iteration.");
            }
        }
    }
}
