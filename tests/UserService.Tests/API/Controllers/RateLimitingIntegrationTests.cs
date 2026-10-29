using System.Net;
using System.Net.Http.Json;
using UserService.API.Controllers;
using UserService.Tests.Infrastructure;

namespace UserService.Tests.API.Controllers;

public sealed class RateLimitingIntegrationTests : IClassFixture<RateLimitingWebApplicationFactory>
{
  private readonly HttpClient _client;

  public RateLimitingIntegrationTests(RateLimitingWebApplicationFactory factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task RegisterUser_WhenLimitIsExceeded_DoesNotBlockUserRead()
  {
    var firstResponse = await RegisterUserAsync($"first-{Guid.NewGuid():N}@example.com");

    Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

    var createdUser = await firstResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

    Assert.NotNull(createdUser);

    var secondResponse = await RegisterUserAsync($"second-{Guid.NewGuid():N}@example.com");

    Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);

    var getUserResponse = await _client.GetAsync($"/api/users/{createdUser.Id}");

    Assert.Equal(HttpStatusCode.OK, getUserResponse.StatusCode);
  }

  private async Task<HttpResponseMessage> RegisterUserAsync(string email)
  {
    var request = new { Email = email, Password = "ValidPassword123!" };

    return await _client.PostAsJsonAsync("/api/users", request);
  }
}