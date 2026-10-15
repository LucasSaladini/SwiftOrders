using System.Net;
using UserService.Tests.Infrastructure;

namespace UserService.Tests.Integration;

public sealed class HealthChecksIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
  private readonly HttpClient _client;

  public HealthChecksIntegrationTests(CustomWebApplicationFactory factory)
  {
    _client = factory.CreateClient();
  }

  [Theory]
  [InlineData("/health/live")]
  [InlineData("/health/ready")]
  public async Task GetHealthEndpoint_ReturnsHealthy(string endpoint)
  {
    // Act
    using var response = await _client.GetAsync(endpoint);
    var body = await response.Content.ReadAsStringAsync();

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("Healthy", body);
    Assert.Equal(
        "text/plain",
        response.Content.Headers.ContentType?.MediaType);
  }
}