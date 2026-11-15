using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Nawly.Abstraction.IServices.Auth;

namespace Nawly.Web.Extensions;

public static class WebSecurityExtensions
{
    private const string FallbackDevSecret = "Nawly-Dev-JWT-Signing-Key-Change-In-Production-2026!";

    public static IServiceCollection AddWebSecurityServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.AddHttpContextAccessor();
        services.Replace(ServiceDescriptor.Scoped<ICurrentUser, HttpCurrentUser>());

        var secret = configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
        {
            if (environment is null || environment.IsDevelopment())
            {
                secret = FallbackDevSecret;
            }
            else
            {
                throw new InvalidOperationException(
                    "Jwt:SecretKey must be configured and at least 32 bytes in non-development environments.");
            }
        }

        var issuer = configuration["Jwt:Issuer"] ?? "Nawly.Api";
        var audience = configuration["Jwt:Audience"] ?? "Nawly.Clients";
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = environment is not null && !environment.IsDevelopment();
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            context.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("Customer", policy => policy.RequireRole("Customer"))
            .AddPolicy("VendorOwner", policy => policy.RequireRole("VendorOwner"))
            .AddPolicy("VendorStaff", policy => policy.RequireRole("VendorStaff"))
            .AddPolicy("VendorAccess", policy => policy.RequireRole("VendorOwner", "VendorStaff", "Admin"))
            .AddPolicy("Courier", policy => policy.RequireRole("Courier"))
            .AddPolicy("Admin", policy => policy.RequireRole("Admin"));

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is null || allowedOrigins.Length == 0)
            allowedOrigins = ["http://localhost:4200", "https://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddPolicy("NawlyCors", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "common.rate_limited",
                    Detail = "Too many requests. Please wait a moment before retrying.",
                    Type = "https://httpstatuses.io/429",
                    Instance = context.HttpContext.Request.Path.Value
                };
                problem.Extensions["code"] = "common.rate_limited";
                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: token);
            };

            options.AddPolicy("auth-otp", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientIpPartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy("auth-login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientIpPartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(5),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy("general-api", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientIpPartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        return services;
    }

    private static string GetClientIpPartitionKey(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
