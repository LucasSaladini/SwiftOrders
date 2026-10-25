using Scalar.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UserService.API.Middleware;
using UserService.Infrastructure;
using UserService.API.HealthChecks;
using UserService.Application.Users.CreateUser;
using UserService.Application.Users.GetUserById;
using UserService.Application.Users.GetAllUsers;
using UserService.Application.Users.GetActiveUsers;
using UserService.Application.Users.GetInactiveUsers;
using UserService.Application.Users.DeleteUser;
using UserService.Application.Users.ReactivateUser;
using UserService.Application.Users.ChangePassword;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddUserServiceInfrastructure(builder.Configuration);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "SwiftOrders - User Service API";
        document.Info.Version = "v1";
        document.Info.Description =
            "API for user registration and management in SwiftOrders.";

        return Task.CompletedTask;
    });
});

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddCheck<DatabaseHealthCheck>(
        "database",
        tags: ["ready"]);

builder.Services.AddControllers();
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<GetUserByIdHandler>();
builder.Services.AddScoped<GetAllUsersHandler>();
builder.Services.AddScoped<GetActiveUsersHandler>();
builder.Services.AddScoped<GetInactiveUsersHandler>();
builder.Services.AddScoped<DeleteUserHandler>();
builder.Services.AddScoped<ReactivateUserHandler>();
builder.Services.AddScoped<ChangePasswordHandler>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.Run();