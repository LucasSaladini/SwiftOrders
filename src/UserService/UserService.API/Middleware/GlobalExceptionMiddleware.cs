using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace UserService.API.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
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