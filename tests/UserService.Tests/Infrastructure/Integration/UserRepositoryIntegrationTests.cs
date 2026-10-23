
using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Persistence.Repositories;

namespace UserService.Tests.Infrastructure.Integration;

public sealed class UserRepositoryIntegrationTests
{
  [Fact]
  public async Task AddAsync_PersistsUser_AndExistsByEmailReturnsTrue()
  {
    var connectionString =
        Environment.GetEnvironmentVariable("SWIFTORDERS_TEST_CONNECTION")
        ?? throw new InvalidOperationException(
            "Set the SWIFTORDERS_TEST_CONNECTION environment variable");

    var options = new DbContextOptionsBuilder<UserDbContext>()
        .UseNpgsql(connectionString)
        .Options;

    var email = $"integration-{Guid.NewGuid():N}@example.com";
    var user = new User(email, "integration-test-password-hash");

    await using var context = new UserDbContext(options);
    var repository = new UserRepository(context);

    try
    {
      await repository.AddAsync(user, CancellationToken.None);

      var exists = await repository.ExistsByEmailAsync(
          email,
          CancellationToken.None);

      Assert.True(exists);

      var persistedUser = await context.Users
          .AsNoTracking()
          .SingleOrDefaultAsync(item => item.Id == user.Id);

      Assert.NotNull(persistedUser);
      Assert.Equal(user.Id, persistedUser.Id);
      Assert.Equal(email, persistedUser.Email.Value);
    }
    finally
    {
      await context.Users
          .Where(item => item.Id == user.Id)
          .ExecuteDeleteAsync();
    }
  }

  [Fact]
  public async Task UpdateAsync_PersistsUserReactivation()
  {
    var connectionString =
        Environment.GetEnvironmentVariable("SWIFTORDERS_TEST_CONNECTION")
        ?? throw new InvalidOperationException(
            "Set the SWIFTORDERS_TEST_CONNECTION environment variable");

    var options = new DbContextOptionsBuilder<UserDbContext>()
        .UseNpgsql(connectionString)
        .Options;

    var user = new User(
        $"reactivation-{Guid.NewGuid():N}@example.com",
        "integration-test-password-hash");

    var userId = user.Id;

    try
    {
      // Arrange: persist an inactive user.
      await using (var context = new UserDbContext(options))
      {
        var repository = new UserRepository(context);

        user.Deactivate(DateTime.UtcNow.AddDays(-5));

        await repository.AddAsync(user, CancellationToken.None);
      }

      // Act: reactivate and persist the user.
      await using (var context = new UserDbContext(options))
      {
        var repository = new UserRepository(context);

        var persistedUser = await repository.GetByIdAsync(
            userId,
            CancellationToken.None);

        Assert.NotNull(persistedUser);
        Assert.False(persistedUser.Active);

        var result = persistedUser.Reactivate(DateTime.UtcNow);

        Assert.True(result.IsSuccess);

        await repository.UpdateAsync(
            persistedUser,
            CancellationToken.None);
      }

      // Assert: verify the persisted state with a fresh context.
      await using (var context = new UserDbContext(options))
      {
        var persistedUser = await context.Users
            .AsNoTracking()
            .SingleAsync(item => item.Id == userId);

        Assert.True(persistedUser.Active);
        Assert.Null(persistedUser.DeletedAt);
      }
    }
    finally
    {
      await using var cleanupContext = new UserDbContext(options);

      await cleanupContext.Users
          .Where(item => item.Id == userId)
          .ExecuteDeleteAsync();
    }
  }

}