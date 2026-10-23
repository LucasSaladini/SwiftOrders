using System.Net;
using UserService.Tests.Infrastructure;

namespace UserService.Tests.Integration;

public sealed class HealthChecksIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthChecksIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetLiveHealthEndpoint_ReturnsHealthy()
    {
        using var response = await _client.GetAsync("/health/live");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
        Assert.Equal(
            "text/plain",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetReadyHealthEndpoint_ReturnsServiceUnavailable_WhenDatabaseIsUnavailable()
    {
        await using var factory =
            new UnavailableDatabaseWebApplicationFactory();

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
        Assert.Equal("Unhealthy", body);
        Assert.Equal(
            "text/plain",
            response.Content.Headers.ContentType?.MediaType);
    }
}