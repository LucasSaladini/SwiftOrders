using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UserService.Infrastructure.Persistence;

namespace UserService.API.HealthChecks;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
  public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
  {
    try
    {
      await using var scope = scopeFactory.CreateAsyncScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
      var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

      return canConnect
        ? HealthCheckResult.Healthy("Database connection is available;")
        : HealthCheckResult.Unhealthy("Database connection is unavailable.");
    }
    catch (Exception exception)
    {
      return HealthCheckResult.Unhealthy("Database threw an exception: ", exception);
    }
  }
}