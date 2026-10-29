
using System.Net;
using System.Net.Http.Json;
using UserService.API.Controllers;
using UserService.Tests.Infrastructure;

namespace UserService.Tests.API.Controllers;

public sealed class ReadRateLimitingIntegrationTests : IClassFixture<ReadRateLimitingWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ReadRateLimitingIntegrationTests(ReadRateLimitingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UserRead_WhenLimitIsExceeded_DoesNotBlockUserWrite()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/users",
            new
            {
                Email = $"read-{Guid.NewGuid():N}@example.com",
                Password = "ValidPassword123!"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        Assert.NotNull(createdUser);

        var firstGetResponse = await GetUserAsync(createdUser.Id, "192.0.2.10");

        Assert.Equal(HttpStatusCode.OK, firstGetResponse.StatusCode);

        var secondGetResponse = await GetUserAsync(createdUser.Id, "192.0.2.10");

        Assert.Equal(HttpStatusCode.TooManyRequests, secondGetResponse.StatusCode);

        var differentIpGetResponse = await GetUserAsync(createdUser.Id, "192.0.2.11");

        Assert.Equal(HttpStatusCode.OK, differentIpGetResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/users/{createdUser.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    private async Task<HttpResponseMessage> GetUserAsync(Guid userId, string clientIp)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}");

        request.Headers.Add("X-Test-Client-IP", clientIp);

        return await _client.SendAsync(request);
    }
}