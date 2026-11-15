using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Nawly.Abstraction.IServices.Auth;
using Nawly.Presentation.Extensions;
using Nawly.Shared.Common.Results;

namespace Nawly.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[EnableRateLimiting("general-api")]
public abstract class ApiBaseController : ControllerBase
{
    protected ICurrentUser CurrentUser => HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

    protected bool TryGetAuthenticatedUserId(out Guid userId)
    {
        if (CurrentUser.UserId is { } id && id != Guid.Empty)
        {
            userId = id;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }

    protected bool TryResolveIdempotencyKey(
        Guid? fallback,
        out Guid idempotencyKey,
        out IActionResult? errorResult,
        string errorCode = "order.invalid_idempotency_key")
    {
        if (Request.Headers.TryGetValue("Idempotency-Key", out var values))
        {
            var raw = values.ToString().Trim();
            if (!string.IsNullOrEmpty(raw))
            {
                if (Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty)
                {
                    idempotencyKey = parsed;
                    errorResult = null;
                    return true;
                }

                idempotencyKey = Guid.Empty;
                errorResult = Result.Failure(Error.Validation(
                    errorCode,
                    "Idempotency-Key header must be a valid non-empty GUID."))
                    .ToActionResult(this);
                return false;
            }
        }

        idempotencyKey = fallback is { } b && b != Guid.Empty ? b : Guid.CreateVersion7();
        errorResult = null;
        return true;
    }

    protected IActionResult UnauthorizedProblem(string message = "Authentication required.")
        => Result.Failure(Error.Unauthorized("auth.unauthorized", message)).ToActionResult(this);

    protected IActionResult HandleResult(Result result)
        => result.ToActionResult(this);

    protected IActionResult HandleResult<T>(Result<T> result, int successStatusCode = StatusCodes.Status200OK)
        => result.ToActionResult(this, successStatusCode);
}
