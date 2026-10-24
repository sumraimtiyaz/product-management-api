using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Api.Extensions;
using ProductManagement.Domain.Exceptions;

namespace ProductManagement.Api.Middleware;

/// <summary>
/// Turns exceptions into RFC 7807 problem responses with a consistent shape. Domain exceptions map to
/// their intended status code; anything unexpected becomes a 500 without leaking internals.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    private const int ClientClosedRequest = 499;

    // Ordered from most to least specific: the first entry the exception is assignable to wins.
    private static readonly (Type ExceptionType, int StatusCode, string Title)[] Mappings =
    [
        (typeof(NotFoundException), StatusCodes.Status404NotFound, "The requested resource was not found."),
        (typeof(ConflictException), StatusCodes.Status409Conflict, "The request conflicts with the current state."),
        (typeof(AuthenticationFailedException), StatusCodes.Status401Unauthorized, "Authentication failed."),
        (typeof(DomainException), StatusCodes.Status400BadRequest, "The request is invalid.")
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("The client cancelled {Method} {Path}.", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = ClientClosedRequest;
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var problem = CreateProblem(exception);
            problem.Instance = context.Request.Path;
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

            context.Response.Clear();
            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                problem, problem.GetType(), options: null, contentType: "application/problem+json");
        }
    }

    private ProblemDetails CreateProblem(Exception exception)
    {
        if (exception is ValidationException validation)
        {
            return validation.Errors.ToProblemDetails();
        }

        var mapping = Mappings.FirstOrDefault(entry => entry.ExceptionType.IsInstanceOfType(exception));
        if (mapping.ExceptionType is not null)
        {
            logger.LogWarning(exception, "Request failed with {StatusCode}: {Message}", mapping.StatusCode, exception.Message);
            return new ProblemDetails { Status = mapping.StatusCode, Title = mapping.Title, Detail = exception.Message };
        }

        logger.LogError(exception, "Unhandled exception while processing the request.");
        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = environment.IsDevelopment() ? exception.ToString() : null
        };
    }
}
