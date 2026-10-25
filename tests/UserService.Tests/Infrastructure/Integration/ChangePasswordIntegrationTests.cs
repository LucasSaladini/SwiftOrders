using Microsoft.EntityFrameworkCore;
using UserService.Application.Users.ChangePassword;
using UserService.Domain.Entities;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Persistence.Repositories;
using UserService.Infrastructure.Security;

namespace UserService.Tests.Infrastructure.Integration;

public sealed class ChangePasswordIntegrationTests
{
  private const string CurrentPassword = "CurrentPassword123!";
  private const string NewPassword = "NewPassword123456!";

  [Fact]
  public async Task ChangePassword_PersistsNewHash_AndNewPasswordCanBeVerified()
  {
    var connectionString = Environment.GetEnvironmentVariable("SWIFTORDERS_TEST_CONNECTION")
                            ?? throw new InvalidOperationException("Set the SWIFTORDERS_TEST_CONNECTION environment variable");
    var options = new DbContextOptionsBuilder<UserDbContext>().UseNpgsql(connectionString).Options;
    var hasher = new IdentityPasswordHasher();
    var user = new User($"change-password-{Guid.NewGuid():N}@example.com", hasher.Hash(CurrentPassword));

    try
    {
      // Arrange: persist the user
      await using (var context = new UserDbContext(options))
      {
        var repository = new UserRepository(context);
        await repository.AddAsync(user, CancellationToken.None);
      }

      // Act: change the password using the real handler and dependencies
      await using (var context = new UserDbContext(options))
      {
        var repository = new UserRepository(context);
        var handler = new ChangePasswordHandler(repository, hasher);
        var result = await handler.Handle(new ChangePasswordCommand(user.Id, CurrentPassword, NewPassword), CancellationToken.None);

        Assert.True(result.IsSuccess);
      }

      // Assert: verify persisted state using a fresh context
      await using (var context = new UserDbContext(options))
      {
        var persistedUser = await context.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);

        Assert.NotEqual(user.PasswordHash, persistedUser.PasswordHash);
        Assert.True(hasher.Verify(persistedUser.PasswordHash, NewPassword));
        Assert.False(hasher.Verify(persistedUser.PasswordHash, CurrentPassword));
      }
    }
    finally
    {
      await using var cleanupContext = new UserDbContext(options);
      await cleanupContext.Users.Where(item => item.Id == user.Id).ExecuteDeleteAsync();
    }
  }

  [Fact]
  public async Task ChangePassword_WithIncorrectCurrentPassword_DoesNotChangePersistedHash()
  {
    var connectionString = Environment.GetEnvironmentVariable("SWIFTORDERS_TEST_CONNECTION")
                            ?? throw new InvalidOperationException("Set the SWIFTORDERS_TEST_CONNECTION environment variable");
    var options = new DbContextOptionsBuilder<UserDbContext>().UseNpgsql(connectionString).Options;
    var hasher = new IdentityPasswordHasher();
    var originalHash = hasher.Hash(CurrentPassword);
    var user = new User($"change-password-failure-{Guid.NewGuid():N}@example.com", originalHash);

    try
    {
      // Arrange
      await using (var context = new UserDbContext(options))
      {
        var repository = new UserRepository(context);
        await repository.AddAsync(user, CancellationToken.None);
      }

      // Act
      await using (var context = new UserDbContext(options))
      {
        var repository = new UserRepository(context);
        var handler = new ChangePasswordHandler(repository, hasher);
        var result = await handler.Handle(new ChangePasswordCommand(user.Id, "WrongPassword123!", NewPassword), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.IncorrectCurrentPassword", result.Error?.Code);
      }

      // Assert: verify the database was not modified
      await using (var context = new UserDbContext(options))
      {
        var persistedUser = await context.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);

        Assert.Equal(originalHash, persistedUser.PasswordHash);
        Assert.True(hasher.Verify(persistedUser.PasswordHash, CurrentPassword));
      }
    }
    finally
    {
      await using var cleanupContext = new UserDbContext(options);
      await cleanupContext.Users.Where(item => item.Id == user.Id).ExecuteDeleteAsync();
    }
  }
}