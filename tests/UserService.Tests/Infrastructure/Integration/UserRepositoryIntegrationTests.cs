
using Microsoft.EntityFrameworkCore;
using UserService.Application.Exceptions;
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

    var options = new DbContextOptionsBuilder<UserDbContext>().UseNpgsql(connectionString).Options;

    var email = $"integration-{Guid.NewGuid():N}@example.com";
    var user = new User(email, "integration-test-password-hash");

    await using var context = new UserDbContext(options);
    var repository = new UserRepository(context);

    try
    {
      await repository.AddAsync(user, CancellationToken.None);

      var exists = await repository.ExistsByEmailAsync(email, CancellationToken.None);

      Assert.True(exists);

      var persistedUser = await context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == user.Id);

      Assert.NotNull(persistedUser);
      Assert.Equal(user.Id, persistedUser.Id);
      Assert.Equal(email, persistedUser.Email.Value);
    }
    finally
    {
      await context.Users.Where(item => item.Id == user.Id).ExecuteDeleteAsync();
    }
  }

  [Fact]
  public async Task UpdateAsync_PersistsUserReactivation()
  {
    var connectionString =
        Environment.GetEnvironmentVariable("SWIFTORDERS_TEST_CONNECTION")
        ?? throw new InvalidOperationException(
            "Set the SWIFTORDERS_TEST_CONNECTION environment variable");

    var options = new DbContextOptionsBuilder<UserDbContext>().UseNpgsql(connectionString).Options;

    var user = new User($"reactivation-{Guid.NewGuid():N}@example.com", "integration-test-password-hash");

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

        var persistedUser = await repository.GetByIdAsync(userId, CancellationToken.None);

        Assert.NotNull(persistedUser);
        Assert.False(persistedUser.Active);

        var result = persistedUser.Reactivate(DateTime.UtcNow);

        Assert.True(result.IsSuccess);

        await repository.UpdateAsync(persistedUser, CancellationToken.None);
      }

      // Assert: verify the persisted state with a fresh context.
      await using (var context = new UserDbContext(options))
      {
        var persistedUser = await context.Users.AsNoTracking().SingleAsync(item => item.Id == userId);

        Assert.True(persistedUser.Active);
        Assert.Null(persistedUser.DeletedAt);
      }
    }
    finally
    {
      await using var cleanupContext = new UserDbContext(options);

      await cleanupContext.Users.Where(item => item.Id == userId).ExecuteDeleteAsync();
    }
  }

  [Fact]
  public async Task UpdateAsync_WhenEmailIsAlreadyUsed_ThrowsEmailAlreadyExistsException()
  {
    var connectionString =
        Environment.GetEnvironmentVariable("SWIFTORDERS_TEST_CONNECTION")
        ?? throw new InvalidOperationException(
            "Set the SWIFTORDERS_TEST_CONNECTION environment variable");

    var options = new DbContextOptionsBuilder<UserDbContext>().UseNpgsql(connectionString).Options;

    var suffix = Guid.NewGuid().ToString("N");
    var firstEmail = $"first-{suffix}@example.com";
    var secondEmail = $"second-{suffix}@example.com";

    var firstUser = new User(firstEmail, "password-hash");
    var secondUser = new User(secondEmail, "password-hash");

    try
    {
      // Arrange: persist two users with different emails.
      await using (var context = new UserDbContext(options))
      {
        var repository = new UserRepository(context);

        await repository.AddAsync(firstUser, CancellationToken.None);
        await repository.AddAsync(secondUser, CancellationToken.None);
      }

      // Act: change the second user's email to the first user's email.
      await using (var context = new UserDbContext(options))
      {
        var repository = new UserRepository(context);

        var persistedUser = await repository.GetByIdAsync(secondUser.Id, CancellationToken.None);

        Assert.NotNull(persistedUser);

        persistedUser.ChangeEmail(firstEmail, DateTime.UtcNow);

        // Assert: PostgreSQL's unique index violation is translated.
        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => repository.UpdateAsync(persistedUser, CancellationToken.None));
      }

      // Assert: the failed update must not change the persisted email.
      await using (var context = new UserDbContext(options))
      {
        var persistedUser = await context.Users.AsNoTracking().SingleAsync(user => user.Id == secondUser.Id);

        Assert.Equal(secondEmail, persistedUser.Email.Value);
      }
    }
    finally
    {
      await using var context = new UserDbContext(options);

      await context.Users.Where(user => user.Id == firstUser.Id || user.Id == secondUser.Id).ExecuteDeleteAsync();
    }
  }

  [Fact]
  public async Task UpdateAsync_WhenUserWasModifiedByAnotherContext_ThrowsUserConcurrencyException()
  {
    var connectionString = Environment.GetEnvironmentVariable("SWIFTORDERS_TEST_CONNECTION")
    ?? throw new InvalidOperationException("Set the SWIFTORDERS_TEST_CONNECTION environment variable");

    var options = new DbContextOptionsBuilder<UserDbContext>().UseNpgsql(connectionString).Options;
    var user = new User($"concurrency-{Guid.NewGuid():N}@example.com", "initial-password-hash");

    try
    {
      // Arrange: persist the user at version 1
      await using (var setupContext = new UserDbContext(options))
      {
        var repository = new UserRepository(setupContext);
        await repository.AddAsync(user, CancellationToken.None);
      }

      // Load the same version into two independent DbContexts
      await using var firstContext = new UserDbContext(options);
      await using var secondContext = new UserDbContext(options);

      var firstRepository = new UserRepository(firstContext);
      var secondRepository = new UserRepository(secondContext);

      var firstUser = await firstRepository.GetByIdAsync(user.Id, CancellationToken.None);
      var secondUser = await secondRepository.GetByIdAsync(user.Id, CancellationToken.None);

      Assert.NotNull(firstUser);
      Assert.NotNull(secondUser);
      Assert.Equal(firstUser.Version, secondUser.Version);

      // The first updated succeeds and increments the version
      firstUser.ChangeEmail($"winner-{Guid.NewGuid():N}@example.com", DateTime.UtcNow);
      await firstRepository.UpdateAsync(firstUser, CancellationToken.None);

      // The second update still uses the original version and must fail
      secondUser.ChangeEmail($"loser-{Guid.NewGuid():N}@example.com", DateTime.UtcNow);
      await Assert.ThrowsAsync<UserConcurrencyException>(() => secondRepository.UpdateAsync(secondUser, CancellationToken.None));

      // Verify that the first update remains persisted
      await using var verificationContext = new UserDbContext(options);
      var persistedUser = await verificationContext.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);

      Assert.Equal(firstUser.Email.Value, persistedUser.Email.Value);
      Assert.Equal(firstUser.Version, persistedUser.Version);
    }
    finally
    {
      await using var cleanupContext = new UserDbContext(options);

      await cleanupContext.Users.Where(item => item.Id == user.Id).ExecuteDeleteAsync();
    }
  }
}