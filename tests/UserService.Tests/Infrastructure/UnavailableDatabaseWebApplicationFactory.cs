using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UserService.Tests.Infrastructure;

public sealed class UnavailableDatabaseWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SWIFTORDERS_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set the SWIFTORDERS_TEST_CONNECTION environment variable");

        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "ConnectionStrings:UserDatabase",
            connectionString);

        builder.ConfigureTestServices(services =>
        {
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                var databaseCheck = options.Registrations.FirstOrDefault(registration => registration.Name == "database");

                if (databaseCheck is not null)
                {
                    options.Registrations.Remove(databaseCheck);
                }

                options.Registrations.Add(
                    new HealthCheckRegistration(
                        "database",
                        _ => new UnavailableDatabaseHealthCheck(),
                        HealthStatus.Unhealthy,
                        ["ready"]));
            });
        });
    }

    private sealed class UnavailableDatabaseHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "Database connection is unavailable."));
        }
    }
}