
using System.Net;
using System.Net.Http.Json;
using UserService.API.Controllers;
using UserService.Tests.Infrastructure;

namespace UserService.Tests.API.Controllers;

public sealed class WriteRateLimitingIntegrationTests : IClassFixture<WriteRateLimitingWebApplicationFactory>
{
  private readonly HttpClient _client;

  public WriteRateLimitingIntegrationTests(WriteRateLimitingWebApplicationFactory factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task UserWrite_WhenLimitIsExceeded_DoesNotBlockUserRead()
  {
    var email = $"write-{Guid.NewGuid():N}@example.com";

    var createResponse = await _client.PostAsJsonAsync("/api/users",
        new
        {
          Email = email,
          Password = "ValidPassword123!"
        });

    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

    var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

    Assert.NotNull(createdUser);

    var firstDeleteResponse = await _client.DeleteAsync($"/api/users/{createdUser.Id}");

    Assert.Equal(HttpStatusCode.NoContent, firstDeleteResponse.StatusCode);

    var reactivateResponse = await _client.PostAsync($"/api/users/{createdUser.Id}/reactivate", content: null);

    Assert.Equal(HttpStatusCode.TooManyRequests, reactivateResponse.StatusCode);

    var getUserResponse = await _client.GetAsync($"/api/users/{createdUser.Id}");

    Assert.Equal(HttpStatusCode.OK, getUserResponse.StatusCode);
  }
}
