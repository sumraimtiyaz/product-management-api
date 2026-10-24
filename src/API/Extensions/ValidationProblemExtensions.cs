using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ProductManagement.Api.Extensions;

public static class ValidationProblemExtensions
{
    /// <summary>
    /// Builds the standard 400 response body for validation failures, so every source of validation
    /// errors (the validation filter, the identity store) produces the same shape.
    /// </summary>
    public static ValidationProblemDetails ToProblemDetails(this IEnumerable<ValidationFailure> failures)
    {
        var errors = failures
            .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        };
    }
}
