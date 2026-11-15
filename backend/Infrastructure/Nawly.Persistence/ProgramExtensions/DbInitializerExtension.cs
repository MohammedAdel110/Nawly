using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nawly.Domain.Contracts.DbInitializer;

namespace Nawly.Persistence.ProgramExtensions;

public static class DbInitializerExtension
{
    public static async Task SeedDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DbInitializerExtension).FullName!);

        try
        {
            var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            await initializer.DataSeedAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Database migration/seeding skipped or failed at startup. Verify PostgreSQL connection string and credentials before serving live traffic.");
        }
    }
}
