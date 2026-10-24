using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProductManagement.Api.Extensions;

namespace ProductManagement.Api.Filters;

/// <summary>
/// Validates every action argument that has a FluentValidation validator registered and short-circuits
/// with a 400 problem response when any rule fails, so controllers only ever see valid input.
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failures = new List<ValidationFailure>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            context.Result = new ObjectResult(failures.ToProblemDetails())
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentTypes = { "application/problem+json" }
            };
            return;
        }

        await next();
    }
}
