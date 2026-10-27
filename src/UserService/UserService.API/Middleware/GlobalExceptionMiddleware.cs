using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Exceptions;

namespace UserService.API.Middleware;

/// <summary>
/// Handles unhandled exceptions thrown while processing HTTP requests.
/// </summary>
/// <param name="next">The next middleware in the request pipeline.</param>
/// <param name="logger">The logger used to record exception details.</param>
public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    /// <summary>
    /// Invokes the next middleware and handles unhandled exceptions.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException exception)
            when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation(
                exception,
                "The request was aborted while processing the request. Trace identifier: {TraceIdentifier}",
                context.TraceIdentifier);

            throw;
        }
        catch (EmailAlreadyExistsException exception)
        {
            logger.LogWarning(exception,
                "A request attempted to register an email that is already in use. Trace identifier: {TraceIdentifier}",
                context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Type = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.10",
                Title = "Email already exists.",
                Status = StatusCodes.Status409Conflict,
                Detail = "The email address is already in use.",
                Instance = context.Request.Path.Value
            };

            await context.Response.WriteAsJsonAsync(
                problemDetails,
                options: (JsonSerializerOptions?)null,
                contentType: "application/problem+json",
                cancellationToken: context.RequestAborted);
        }
        catch (UserConcurrencyException exception)
        {
            logger.LogWarning(exception,
                "A request attempted to update a user that was modified concurrently. Trace identifier: {TraceIdentifier}",
                context.TraceIdentifier);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Type = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.10",
                Title = "User was modified.",
                Status = StatusCodes.Status409Conflict,
                Detail = "The user was modified by another operation. Reload the resource and retry.",
                Instance = context.Request.Path.Value
            };

            await context.Response.WriteAsJsonAsync(
                problemDetails,
                options: (JsonSerializerOptions?)null,
                contentType: "application/problem+json",
                cancellationToken: context.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "An unhandled exception occurred while processing the request. TraceIdentifier: {TraceIdentifier}",
                context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Type = "https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1",
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred while processing your request.",
                Instance = context.Request.Path.Value
            };

            await context.Response.WriteAsJsonAsync(
                problemDetails,
                options: (JsonSerializerOptions?)null,
                contentType: "application/problem+json",
                cancellationToken: context.RequestAborted);
        }
    }
}
