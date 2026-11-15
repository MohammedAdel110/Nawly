using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Nawly.Presentation.Filters;

/// <summary>
/// Automatically runs registered FluentValidation validators for controller action arguments
/// and returns an RFC 9457 ValidationProblemDetails response on failure.
/// </summary>
public sealed class ValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null)
                continue;

            var argType = arg.GetType();
            if (argType.IsPrimitive || arg is string || arg is Guid || arg is CancellationToken)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argType);
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContextType = typeof(ValidationContext<>).MakeGenericType(argType);
            if (Activator.CreateInstance(validationContextType, arg) is not IValidationContext validationContext)
                continue;

            var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

                var problemDetails = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Detail = "Request validation failed.",
                    Type = "https://httpstatuses.io/400",
                    Instance = context.HttpContext.Request.Path.Value
                };

                problemDetails.Extensions["code"] = "common.validation_failed";
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                context.Result = new BadRequestObjectResult(problemDetails);
                return;
            }
        }

        await next();
    }
}
