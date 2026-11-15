namespace Nawly.Web.Middleware;

/// <summary>
/// Appends defensive security response headers across API responses.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");
            return Task.CompletedTask;
        });

        return next(context);
    }
}
