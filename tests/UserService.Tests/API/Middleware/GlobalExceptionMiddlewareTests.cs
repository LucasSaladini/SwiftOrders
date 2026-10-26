using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Http.Features;
using UserService.API.Middleware;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Exceptions;

namespace UserService.Tests.API.Middleware;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNextThrows_ReturnsInternalServerError()
    {
        // Arrange
        RequestDelegate next = _ => throw new InvalidOperationException("Sensitive internal details");
        var middleware = new GlobalExceptionMiddleware(next, NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();

        context.Response.Body = new MemoryStream();
        context.Request.Path = "/users";

        // Act
        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;

        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Assert
        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemDetails.Status);
        Assert.Equal("An unexpected error occurred.", problemDetails.Title);
        Assert.Equal("An unexpected error occurred while processing your request.", problemDetails.Detail);
        Assert.Equal("/users", problemDetails.Instance);
        Assert.NotEqual("Sensitive internal details", problemDetails.Detail);

        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.DoesNotContain("Sensitive internal details", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_LogsException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database connection failed.");

        RequestDelegate next = _ => throw expectedException;

        var logger = new TestLogger<GlobalExceptionMiddleware>();
        var middleware = new GlobalExceptionMiddleware(next, logger);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "test-trace-123"
        };

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var logEntry = Assert.Single(logger.Entries);

        Assert.Equal(LogLevel.Error, logEntry.LogLevel);
        Assert.Equal("An unhandled exception occurred while processing the request. TraceIdentifier: test-trace-123", logEntry.Message);
        Assert.Same(expectedException, logEntry.Exception);
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasStarted_RethrowsException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Processing failed");

        RequestDelegate next = _ => throw expectedException;

        var middleware = new GlobalExceptionMiddleware(
            next,
            NullLogger<GlobalExceptionMiddleware>.Instance);

        var context = new DefaultHttpContext();
        var responseFeature = new StartedHttpResponseFeature();

        context.Features.Set<IHttpResponseFeature>(responseFeature);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        // Assert
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextSucceeds_DoesNotInterfereWithResponse()
    {
        // Arrange
        var nextExecuted = false;

        RequestDelegate next = context =>
        {
            nextExecuted = true;
            context.Response.StatusCode = StatusCodes.Status200OK;

            return Task.CompletedTask;
        };

        var logger = new TestLogger<GlobalExceptionMiddleware>();
        var middleware = new GlobalExceptionMiddleware(next, logger);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextExecuted);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestAbortedAndNextThrowsOperationCanceledException_LogsCancellationAndRethrows()
    {
        // Arrange
        var expectedException = new OperationCanceledException("Request was canceled");
        var context = new DefaultHttpContext();
        using var cancellationTokenSource = new CancellationTokenSource();

        context.RequestAborted = cancellationTokenSource.Token;
        cancellationTokenSource.Cancel();

        RequestDelegate next = _ => throw expectedException;

        var logger = new TestLogger<GlobalExceptionMiddleware>();
        var middleware = new GlobalExceptionMiddleware(next, logger);

        // Act
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));

        // Assert
        Assert.Same(expectedException, exception);

        var logEntry = Assert.Single(logger.Entries);

        Assert.NotEqual(LogLevel.Error, logEntry.LogLevel);
        Assert.Contains("aborted", logEntry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(expectedException, logEntry.Exception);
    }

    [Fact]
    public async Task InvokeAsync_EmailAlreadyExistsException_ReturnsConflictProblemDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/users/123/email";
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw new EmailAlreadyExistsException("Email is already in use.", new InvalidOperationException("Sensitive database details"));

        var middleware = new GlobalExceptionMiddleware(next, NullLogger<GlobalExceptionMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;

        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var problem = document.RootElement;

        Assert.Equal("Email already exists.", problem.GetProperty("title").GetString());

        Assert.Equal(StatusCodes.Status409Conflict, problem.GetProperty("status").GetInt32());

        Assert.Equal("The email address is already in use.", problem.GetProperty("detail").GetString());

        Assert.Equal("/api/users/123/email", problem.GetProperty("instance").GetString());

        Assert.DoesNotContain("Sensitive database details", problem.ToString());
    }
}

internal sealed class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
    }
}

internal sealed class StartedHttpResponseFeature : IHttpResponseFeature
{
    public int StatusCode { get; set; } = StatusCodes.Status200OK;

    public string? ReasonPhrase { get; set; }

    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

    public Stream Body { get; set; } = new MemoryStream();

    public bool HasStarted => true;

    public void OnStarting(Func<object, Task> callback, object state)
    {
    }

    public void OnCompleted(Func<object, Task> callback, object state)
    {
    }
}

internal sealed record LogEntry(LogLevel LogLevel, EventId EventId, string Message, Exception? Exception);