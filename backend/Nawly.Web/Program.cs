using System.Text.Json.Serialization;
using Nawly.Persistence.ProgramExtensions;
using Nawly.Presentation.Filters;
using Nawly.Services;
using Nawly.Web.Extensions;
using Nawly.Web.Middleware;
using Scalar.AspNetCore;

namespace Nawly.Web
{
    /// <summary>
    /// Nawly API – composition root.
    /// Service registrations will be added through extension methods in
    /// Nawly.Web/Extensions and Nawly.Persistence/ProgramExtensions as features are built.
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ==========================================
            // 1. Database, Identity & Infrastructure Adapters
            // 2. Application Services
            // 3. Security, CORS, Rate limiting
            // ==========================================
            builder.Services.AddPersistenceServices(builder.Configuration);
            builder.Services.AddApplicationServices();
            builder.Services.AddWebSecurityServices(builder.Configuration, builder.Environment);
            builder.Services.AddWebRealtimeAndWorkers();

            // ==========================================
            // 4. Controllers & Global Validation Filter
            // ==========================================
            builder.Services.AddControllers(options => options.Filters.Add<ValidationActionFilter>())
                .AddApplicationPart(typeof(Nawly.Presentation.AssemblyReference).Assembly)
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
                });

            // ==========================================
            // 5. OpenAPI (Development only, per SRS §3.2)
            // ==========================================
            builder.Services.AddOpenApi();

            var app = builder.Build();

            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();

            await app.Services.SeedDatabaseAsync();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseCors("NawlyCors");
            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapNawlyHubs();

            app.Run();
        }
    }
}
