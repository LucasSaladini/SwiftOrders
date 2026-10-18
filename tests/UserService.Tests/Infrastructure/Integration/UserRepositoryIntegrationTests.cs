
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
}