using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Nawly.Web.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            var candidate = values.ToString().Trim();
            if (candidate.Length is > 0 and <= 64 &&
                candidate.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.'))
            {
                return candidate;
            }
        }

        return Guid.CreateVersion7().ToString("N");
    }
}
