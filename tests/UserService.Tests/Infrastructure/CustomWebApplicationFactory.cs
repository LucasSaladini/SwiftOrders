
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UserService.Tests.Infrastructure;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
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
    }
}