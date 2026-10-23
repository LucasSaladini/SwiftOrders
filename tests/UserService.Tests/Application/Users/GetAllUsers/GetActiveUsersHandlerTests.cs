
using UserService.Application.Abstractions;
using UserService.Application.Users.GetActiveUsers;
using UserService.Domain.Entities;

namespace UserService.Tests.Application.Users.GetActiveUsers;

public class GetActiveUsersHandlerTests
{
  [Fact]
  public async Task Handle_WhenActiveUsersExist_ReturnsActiveUsers()
  {
    // Arrange
    var firstUser = new User(
        "user1@email.com",
        "fake-password-hash");

    var secondUser = new User(
        "user2@email.com",
        "fake-password-hash");

    var inactiveUser = new User(
        "inactive@email.com",
        "fake-password-hash");

    inactiveUser.Deactivate(DateTime.UtcNow);

    var repository = new FakeUserRepository(
        [firstUser, secondUser, inactiveUser]);

    var handler = new GetActiveUsersHandler(repository);

    // Act
    var result = await handler.Handle(CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal(2, result.Value.Count);

    var firstResponse = Assert.Single(result.Value, user => user.Id == firstUser.Id);

    Assert.Equal("user1@email.com", firstResponse.Email);
    Assert.True(firstResponse.Active);
    Assert.Null(firstResponse.DeletedAt);

    var secondResponse = Assert.Single(result.Value, user => user.Id == secondUser.Id);

    Assert.Equal("user2@email.com", secondResponse.Email);
    Assert.True(secondResponse.Active);

    Assert.DoesNotContain(result.Value, user => user.Id == inactiveUser.Id);
  }

  [Fact]
  public async Task Handle_WhenNoActiveUsersExist_ReturnsEmptyList()
  {
    // Arrange
    var inactiveUser = new User(
        "inactive@email.com",
        "fake-password-hash");

    inactiveUser.Deactivate(DateTime.UtcNow);

    var repository = new FakeUserRepository([inactiveUser]);
    var handler = new GetActiveUsersHandler(repository);

    // Act
    var result = await handler.Handle(CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Empty(result.Value);
  }

  private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
  {
    public Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken)
    {
      return Task.FromResult<IReadOnlyList<User>>(users.Where(user => user.Active).ToList());
    }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
      throw new NotSupportedException();
    }

    public Task AddAsync(
        User user,
        CancellationToken cancellationToken)
    {
      throw new NotSupportedException();
    }

    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
    {
      return Task.FromResult(users);
    }

    public Task<IReadOnlyList<User>> GetInactiveAsync(CancellationToken cancellationToken)
    {
      return Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }
  }
}
