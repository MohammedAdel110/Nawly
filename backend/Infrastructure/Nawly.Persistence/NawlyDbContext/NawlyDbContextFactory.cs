using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Nawly.Persistence.Contexts;

public sealed class NawlyDbContextFactory : IDesignTimeDbContextFactory<NawlyDbContext>
{
    public NawlyDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var webPath = Path.Combine(basePath, "backend", "Nawly.Web");
        if (!Directory.Exists(webPath))
            webPath = Path.GetFullPath(Path.Combine(basePath, "..", "Nawly.Web"));
        if (!Directory.Exists(webPath))
            webPath = basePath;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(webPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=nawly;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<NawlyDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(NawlyDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention();

        return new NawlyDbContext(optionsBuilder.Options);
    }
}
