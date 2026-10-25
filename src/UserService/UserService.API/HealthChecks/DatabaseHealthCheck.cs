using Microsoft.Extensions.Diagnostics.HealthChecks;
using UserService.Infrastructure.Persistence;

namespace UserService.API.HealthChecks;

/// <summary>
/// Checks whether the application can connect to the configured database.
/// </summary>
/// <param name="scopeFactory">
/// Factory used to create dependency injection scopes.
/// </param>
public sealed class DatabaseHealthCheck(
    IServiceScopeFactory scopeFactory) : IHealthCheck
{
  /// <summary>
  /// Executes the database connectivity check.
  /// </summary>
  /// <param name="context">
  /// The health check execution context.
  /// </param>
  /// <param name="cancellationToken">
  /// Token used to cancel the operation.
  /// </param>
  /// <returns>
  /// A healthy result if the database is reachable; otherwise, an unhealthy result.
  /// </returns>
  public async Task<HealthCheckResult> CheckHealthAsync(
      HealthCheckContext context,
      CancellationToken cancellationToken)
  {
    try
    {
      await using var scope = scopeFactory.CreateAsyncScope();

      var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
      var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

      return canConnect
          ? HealthCheckResult.Healthy("Database connection is available.")
          : HealthCheckResult.Unhealthy("Database connection is unavailable.");
    }
    catch (OperationCanceledException)
        when (cancellationToken.IsCancellationRequested)
    {
      throw;
    }
    catch (Exception exception)
    {
      return HealthCheckResult.Unhealthy("Database connection check failed.", exception);
    }
  }
}
