using Microsoft.OpenApi;
using Nawly.Persistence.Contexts;
using Scalar.AspNetCore;

namespace Nawly.Web.Extensions;

/// <summary>
/// Configures OpenAPI JWT Bearer security documentation, Scalar UI navigation,
/// and liveness/readiness health endpoints.
/// </summary>
public static class WebObservabilityExtensions
{
    private const string BearerSchemeId = "Bearer";

    public static IServiceCollection AddWebObservabilityServices(this IServiceCollection services)
    {
        services.AddHealthChecks();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Nawly Delivery Platform API (Nawa, Qalyubia)";
                document.Info.Version = "v1";

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes[BearerSchemeId] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "Enter JWT Bearer token."
                };

                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(BearerSchemeId, document)] = []
                });

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication MapNawlyObservabilityEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Nawly Delivery Platform API v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Nawly API — Swagger UI";
                options.EnablePersistAuthorization();
                options.DisplayRequestDuration();
            });

            app.MapScalarApiReference(options =>
            {
                options.WithTitle("Nawly Delivery Platform API Reference");
                options.AddPreferredSecuritySchemes(BearerSchemeId);
            });

            // Opening http://localhost:5082/ directly in a browser opens Swagger UI.
            app.MapGet("/", () => Results.Redirect("/swagger"))
                .AllowAnonymous()
                .ExcludeFromDescription();
        }

        app.MapHealthChecks("/health")
            .AllowAnonymous()
            .ExcludeFromDescription();

        app.MapGet("/health/ready", async (NawlyDbContext dbContext, CancellationToken ct) =>
        {
            var canConnect = await dbContext.Database.CanConnectAsync(ct);
            var payload = new
            {
                status = canConnect ? "Healthy" : "Unhealthy",
                database = canConnect ? "Connected" : "Unreachable",
                timestampUtc = DateTimeOffset.UtcNow
            };

            return canConnect
                ? Results.Ok(payload)
                : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        return app;
    }
}
