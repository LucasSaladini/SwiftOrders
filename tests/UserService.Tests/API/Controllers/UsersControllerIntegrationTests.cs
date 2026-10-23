
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserService.API.Controllers;
using UserService.Infrastructure.Persistence;
using UserService.Tests.Infrastructure;

namespace UserService.Tests.API.Controllers;

public sealed class UsersControllerIntegrationTests(
    CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IServiceScopeFactory _scopeFactory =
        factory.Services.GetRequiredService<IServiceScopeFactory>();

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithLocation_AndUserCanBeRetrieved()
    {
        var email = $"controller-{Guid.NewGuid():N}@example.com";
        Guid? createdUserId = null;

        try
        {
            var request = new CreateUserRequest(
                email,
                "StrongTestPassword123!");

            var createResponse = await _client.PostAsJsonAsync(
                "/api/users",
                request);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.NotNull(createResponse.Headers.Location);

            var createdUser = await createResponse.Content
                .ReadFromJsonAsync<CreateUserResponse>();

            Assert.NotNull(createdUser);
            Assert.NotEqual(Guid.Empty, createdUser.Id);

            createdUserId = createdUser.Id;

            var location = createResponse.Headers.Location!;

            Assert.EndsWith(
                $"/api/users/{createdUser.Id}",
                location.ToString(),
                StringComparison.OrdinalIgnoreCase);

            var getResponse = await _client.GetAsync(location);

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            using var json = JsonDocument.Parse(
                await getResponse.Content.ReadAsStringAsync());

            var root = json.RootElement;

            Assert.Equal(createdUser.Id, root.GetProperty("id").GetGuid());
            Assert.Equal(email, root.GetProperty("email").GetString());
            Assert.True(root.GetProperty("active").GetBoolean());
            Assert.True(root.GetProperty("createdAt").GetDateTime() > DateTime.MinValue);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("deletedAt").ValueKind);

            Assert.False(root.TryGetProperty("password", out _));
            Assert.False(root.TryGetProperty("passwordHash", out _));
        }
        finally
        {
            if (createdUserId.HasValue)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var dbContext = scope.ServiceProvider
                    .GetRequiredService<UserDbContext>();

                await dbContext.Users
                    .Where(user => user.Id == createdUserId.Value)
                    .ExecuteDeleteAsync();
            }
        }
    }

    [Fact]
    public async Task Create_DuplicateEmail_ReturnsConflict()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";

        Guid? createdUserId = null;

        try
        {
            var request = new CreateUserRequest(email, "ValidPassword123!");
            var firstResponse = await _client.PostAsJsonAsync("/api/users", request);

            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

            var createdUser = await firstResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

            Assert.NotNull(createdUser);
            createdUserId = createdUser.Id;

            var duplicateResponse = await _client.PostAsJsonAsync("/api/users", request);

            Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

            using var json = JsonDocument.Parse(await duplicateResponse.Content.ReadAsStringAsync());
            var root = json.RootElement;

            Assert.Equal("Email already exists.", root.GetProperty("title").GetString());
            Assert.Equal("An account with this email already exists", root.GetProperty("detail").GetString());
        }
        finally
        {
            if (createdUserId.HasValue)
            {
                await DeleteUserAsync(createdUserId.Value);
            }
        }
    }


    [Fact]
    public async Task Create_PasswordShorterThan15Characters_ReturnsBadRequest()
    {
        var request = new CreateUserRequest($"short-password-{Guid.NewGuid():N}@example.com", "Short123!");
        var response = await _client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("Invalid user data", root.GetProperty("title").GetString());
        Assert.Equal("Password must contain at least 15 characters.", root.GetProperty("detail").GetString());
    }


    [Fact]
    public async Task Create_InvalidEmail_ReturnsBadRequest()
    {
        var request = new CreateUserRequest("invalid-email", "StrongTestPassword123!");
        var response = await _client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("Invalid user data", root.GetProperty("title").GetString());
        Assert.Equal("Email is invalid", root.GetProperty("detail").GetString());
    }


    [Fact]
    public async Task GetUserById_NonexistentUser_ReturnsNotFound()
    {
        var nonexistentUserId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/users/{nonexistentUserId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("User not found", root.GetProperty("title").GetString());
        Assert.Equal("User was not found.", root.GetProperty("detail").GetString());
    }


    [Fact]
    public async Task GetUserById_EmptyGuid_ReturnsBadRequest()
    {
        var response = await _client.GetAsync($"/api/users/{Guid.Empty}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("Invalid user ID", root.GetProperty("title").GetString());
    }


    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_EmptyEmail_ReturnsBadRequest(string email)
    {
        var request = new CreateUserRequest(email, "StrongTestPassword123!");
        var response = await _client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("Invalid user data", root.GetProperty("title").GetString());
        Assert.Equal("Email is required.", root.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task DeleteUser_ExistingUser_ReturnsNoContent_AndUserBecamosInactive()
    {
        var email = $"delete-{Guid.NewGuid():N}@example.com";

        Guid? createdUserId = null;

        try
        {
            // Arrange: creates an user through the API
            var request = new CreateUserRequest(email, "ValidPassword123!");
            var createResponse = await _client.PostAsJsonAsync("/api/users", request);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

            Assert.NotNull(createdUser);
            createdUserId = createdUser.Id;

            // Act: Request logical deletion
            var deleteResponse = await _client.DeleteAsync($"/api/users/{createdUser.Id}");

            // Assert: API returns 204
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Confirms that the user is still searchable
            var getResponse = await _client.GetAsync($"/api/users/{createdUser.Id}");

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            using var json = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
            var root = json.RootElement;

            Assert.Equal(createdUser.Id, root.GetProperty("id").GetGuid());
            Assert.False(root.GetProperty("active").GetBoolean());
            Assert.NotEqual(JsonValueKind.Null, root.GetProperty("deletedAt").ValueKind);
            Assert.Equal(root.GetProperty("deletedAt").GetDateTime(), root.GetProperty("updatedAt").GetDateTime());

            // Confirms that the user still appears on inactive list
            var inactiveResponse = await _client.GetAsync("/api/users/inactive");
            Assert.Equal(HttpStatusCode.OK, inactiveResponse.StatusCode);

            using var inactiveJson = JsonDocument.Parse(await inactiveResponse.Content.ReadAsStringAsync());
            var inactiveUsers = inactiveJson.RootElement.EnumerateArray().ToList();

            Assert.Contains(inactiveUsers, user => user.GetProperty("id").GetGuid() == createdUser.Id);
        }
        finally
        {
            if (createdUserId.HasValue)
            {
                await DeleteUserAsync(createdUserId.Value);
            }
        }
    }

    [Fact]
    public async Task DeleteUser_NonexistentUser_ReturnsNotFound()
    {
        // Arrange
        var nonexistentUserId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/users/{nonexistentUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("User not found", root.GetProperty("title").GetString());
        Assert.Equal("User was not found.", root.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ReactivateUser_RecentlyDeletedUser_ReturnsNoContent_AndUserBecomesActive()
    {
        var email = $"reactivate-{Guid.NewGuid():N}@example.com";
        Guid? createdUserId = null;

        try
        {
            // Arrange: create a user through the API.
            var request = new CreateUserRequest(email, "ValidPassword123!");

            var createResponse = await _client.PostAsJsonAsync("/api/users", request);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

            Assert.NotNull(createdUser);
            createdUserId = createdUser.Id;

            // Act: deactivate and reactivate the user.
            var deleteResponse = await _client.DeleteAsync(
                $"/api/users/{createdUser.Id}");

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            var reactivateResponse = await _client.PostAsync($"/api/users/{createdUser.Id}/reactivate",
                content: null);

            // Assert: the API returns 204 No Content.
            Assert.Equal(HttpStatusCode.NoContent, reactivateResponse.StatusCode);

            // Verify the persisted user state.
            var getResponse = await _client.GetAsync($"/api/users/{createdUser.Id}");

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            using var json = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

            var root = json.RootElement;

            Assert.Equal(createdUser.Id, root.GetProperty("id").GetGuid());
            Assert.True(root.GetProperty("active").GetBoolean());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("deletedAt").ValueKind);
        }
        finally
        {
            if (createdUserId.HasValue)
            {
                await DeleteUserAsync(createdUserId.Value);
            }
        }
    }

    [Fact]
    public async Task ReactivateUser_NonexistentUser_ReturnsNotFound()
    {
        var nonexistentUserId = Guid.NewGuid();

        var response = await _client.PostAsync($"/api/users/{nonexistentUserId}/reactivate", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var root = json.RootElement;

        Assert.Equal("User not found", root.GetProperty("title").GetString());
        Assert.Equal("User was not found.", root.GetProperty("detail").GetString());
    }


    [Fact]
    public async Task ReactivateUser_ActiveUser_ReturnsConflict()
    {
        var email = $"active-reactivate-{Guid.NewGuid():N}@example.com";
        Guid? createdUserId = null;

        try
        {
            var request = new CreateUserRequest(email, "ValidPassword123!");

            var createResponse = await _client.PostAsJsonAsync("/api/users", request);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

            Assert.NotNull(createdUser);
            createdUserId = createdUser.Id;

            var response = await _client.PostAsync($"/api/users/{createdUser.Id}/reactivate", content: null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            var root = json.RootElement;

            Assert.Equal("User cannot be reactivated", root.GetProperty("title").GetString());
            Assert.Equal("User is already active.", root.GetProperty("detail").GetString());
        }
        finally
        {
            if (createdUserId.HasValue)
            {
                await DeleteUserAsync(createdUserId.Value);
            }
        }
    }


    [Fact]
    public async Task ReactivateUser_ExpiredWindow_ReturnsConflict()
    {
        var email = $"expired-reactivate-{Guid.NewGuid():N}@example.com";
        Guid? createdUserId = null;

        try
        {
            // Arrange: create and deactivate a user.
            var request = new CreateUserRequest(email, "ValidPassword123!");

            var createResponse = await _client.PostAsJsonAsync("/api/users", request);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

            Assert.NotNull(createdUser);
            createdUserId = createdUser.Id;

            var deleteResponse = await _client.DeleteAsync($"/api/users/{createdUser.Id}");

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Force the deactivation timestamp to be older than 15 days.
            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();

                var expiredDate = DateTime.UtcNow.AddDays(-16);

                await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE users SET \"DeletedAt\" = {expiredDate} WHERE \"Id\" = {createdUser.Id}");
            }

            // Act: attempt to reactivate the expired account.
            var response = await _client.PostAsync($"/api/users/{createdUser.Id}/reactivate", content: null);

            // Assert: the API rejects the reactivation.
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            var root = json.RootElement;

            Assert.Equal("User cannot be reactivated", root.GetProperty("title").GetString());

            Assert.Equal("The user reactivation window has expired.", root.GetProperty("detail").GetString());

            // Confirm the user remains inactive in the database.
            await using var verificationScope = _scopeFactory.CreateAsyncScope();

            var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<UserDbContext>();

            var persistedUser = await verificationDbContext.Users
                .AsNoTracking()
                .SingleAsync(user => user.Id == createdUser.Id);

            Assert.False(persistedUser.Active);
            Assert.NotNull(persistedUser.DeletedAt);
        }
        finally
        {
            if (createdUserId.HasValue)
            {
                await DeleteUserAsync(createdUserId.Value);
            }
        }
    }

    [Fact]
    public async Task ReactivateUser_MissingDeactivationTimestamp_ReturnsConflict()
    {
        // Arrange: create a user through the API
        var createResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            email = $"reactivate-{Guid.NewGuid()}@example.com",
            password = "TestPassword123!"
        });

        createResponse.EnsureSuccessStatusCode();

        var createdUser = await createResponse.Content.ReadFromJsonAsync<CreateUserResponse>();

        Assert.NotNull(createdUser);

        var userId = createdUser.Id;

        try
        {
            // Deactivate the user through the API
            var deleteResponse = await _client.DeleteAsync($"/api/users/{userId}");

            Assert.True(deleteResponse.IsSuccessStatusCode, $"Expected successful deactivation, received {(int)deleteResponse.StatusCode}.");

            // Simulate inconsistent persisted state:
            // Active = false and DeletedAt = null.
            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();

                await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE users SET \"DeletedAt\" = NULL WHERE \"Id\" = {userId}");
            }

            // Act
            var response = await _client.PostAsync($"/api/users/{userId}/reactivate", content: null);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();

                var user = await dbContext.Users
                    .AsNoTracking()
                    .SingleAsync(user => user.Id == userId);

                Assert.False(user.Active);
                Assert.Null(user.DeletedAt);
            }
        }
        finally
        {
            await DeleteUserAsync(userId);
        }
    }


    private async Task DeleteUserAsync(Guid userId)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        await dbContext.Users.Where(user => user.Id == userId).ExecuteDeleteAsync();
    }
}