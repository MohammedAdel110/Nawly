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

    protected IActionResult UnauthorizedProblem(string message = "Authentication required.")
        => Result.Failure(Error.Unauthorized("auth.unauthorized", message)).ToActionResult(this);

    protected IActionResult HandleResult(Result result)
        => result.ToActionResult(this);

    protected IActionResult HandleResult<T>(Result<T> result, int successStatusCode = StatusCodes.Status200OK)
        => result.ToActionResult(this, successStatusCode);
}
