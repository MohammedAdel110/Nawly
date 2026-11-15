using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nawly.Domain.Contracts.DbInitializer;
using Nawly.Domain.Models.Identity;
using Nawly.Persistence.Seeds;
using AppDbContext = Nawly.Persistence.Contexts.NawlyDbContext;

namespace Nawly.Persistence.Implementations.InitializerImplement;

public sealed class DbInitialized(
    AppDbContext dbContext,
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    IConfiguration configuration,
    ILogger<DbInitialized> logger) : IDbInitializer
{
    public async Task DataSeedAsync(CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.IsRelational())
        {
            var pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pending.Any())
            {
                logger.LogInformation("Applying pending database migrations: {Migrations}", string.Join(", ", pending));
                await dbContext.Database.MigrateAsync(cancellationToken);
            }
        }

        await SeederAsync.SeedAsync(dbContext, roleManager, userManager, configuration, logger, cancellationToken);
    }
}
