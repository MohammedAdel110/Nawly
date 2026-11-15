using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nawly.Shared.Common.Results;

namespace Nawly.Presentation.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
            return controller.NoContent();

        return CreateProblemResult(result.Error, controller);
    }

    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return successStatusCode == StatusCodes.Status200OK
                ? controller.Ok(result.Value)
                : controller.StatusCode(successStatusCode, result.Value);
        }

        return CreateProblemResult(result.Error, controller);
    }

    private static ObjectResult CreateProblemResult(Error error, ControllerBase controller)
    {
        var problemDetails = new ProblemDetails
        {
            Status = error.StatusCode,
            Title = error.Code,
            Detail = error.Message,
            Type = $"https://httpstatuses.io/{error.StatusCode}",
            Instance = controller.HttpContext?.Request.Path.Value
        };

        problemDetails.Extensions["code"] = error.Code;

        if (controller.HttpContext is not null)
            problemDetails.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;

        return new ObjectResult(problemDetails)
        {
            StatusCode = error.StatusCode
        };
    }
}
