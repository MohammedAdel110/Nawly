using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Abstraction.IServices.Notification;
using Nawly.Abstraction.IServices.Payment;
using Nawly.Abstraction.IServices.Sms;
using Nawly.Abstraction.IServices.Storage;
using Nawly.Domain.Contracts.DbInitializer;
using Nawly.Domain.Contracts.GenericReposPattern;
using Nawly.Domain.Contracts.SpecificReposPattern;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Domain.Models.Identity;
using Nawly.Persistence.Adapters;
using Nawly.Persistence.Implementations.InitializerImplement;
using Nawly.Persistence.Implementations.ReposPattern;
using Nawly.Persistence.Implementations.SpecificReposPattern;
using Nawly.Persistence.Implementations.UoWPattern;
using Nawly.Persistence.Interceptors;
using AppDbContext = Nawly.Persistence.Contexts.NawlyDbContext;

namespace Nawly.Persistence.ProgramExtensions;

public static class PersistenceServiceExtensions
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddScoped<AuditableAndSoftDeleteInterceptor>();
        services.AddScoped<OutboxDomainEventsInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                    .EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null));

            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableAndSoftDeleteInterceptor>(),
                serviceProvider.GetRequiredService<OutboxDomainEventsInterceptor>());
        });

        services.AddIdentityCore<AppUser>(options =>
        {
            options.User.RequireUniqueEmail = false;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepo<,>), typeof(GenericRepo<,>));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<ILedgerRepository, LedgerRepository>();
        services.AddScoped<IStoreRepository, StoreRepository>();

        services.AddScoped<IDbInitializer, DbInitialized>();

        // Fallback infrastructure adapters; production/web implementations override automatically when registered earlier.
        services.TryAddScoped<ICurrentUser, DefaultCurrentUser>();
        services.TryAddSingleton<ISmsSender, NullSmsSender>();
        services.TryAddSingleton<IFileStorage, LocalDiskFileStorage>();
        services.TryAddSingleton<IRealtimeNotifier, NullRealtimeNotifier>();
        services.TryAddSingleton<IPaymentGateway, SandboxPaymentGateway>();

        return services;
    }
}
