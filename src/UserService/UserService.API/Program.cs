using Scalar.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UserService.API.Middleware;
using UserService.Infrastructure;
using UserService.API.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddUserServiceInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddCheck<DatabaseHealthCheck>(
        "database",
        tags: ["ready"]);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

public partial class Program { }