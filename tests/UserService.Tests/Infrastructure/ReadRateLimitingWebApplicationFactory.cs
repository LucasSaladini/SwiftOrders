using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UserService.Tests.Infrastructure;

public sealed class ReadRateLimitingWebApplicationFactory : WebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    var connectionString =
        Environment.GetEnvironmentVariable("SWIFTORDERS_TEST_CONNECTION")
        ?? throw new InvalidOperationException(
            "Set the SWIFTORDERS_TEST_CONNECTION environment variable");

    builder.UseEnvironment("Testing");

    builder.ConfigureAppConfiguration((_, configuration) =>
    {
      configuration.AddInMemoryCollection(
        new Dictionary<string, string?>
        {
          ["RateLimiting:api-read:PermitLimit"] = "1",
          ["RateLimiting:user-registration:PermitLimit"] = "10000",
          ["RateLimiting:user-write:PermitLimit"] = "10000"
        });
    });

    builder.UseSetting("ConnectionStrings:UserDatabase", connectionString);
    builder.ConfigureServices(services =>
    {
      services.AddTransient<IStartupFilter, SimulatedClientIpStartupFilter>();
    });
  }
}
