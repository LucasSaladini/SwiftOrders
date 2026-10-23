
using UserService.Application.Abstractions;
using UserService.Application.Users.GetInactiveUsers;
using UserService.Domain.Entities;

namespace UserService.Tests.Application.Users.GetInactiveUsers;

public class GetInactiveUsersHandlerTests
{
  [Fact]
  public async Task Handle_WhenInactiveUsersExist_ReturnsInactiveUsers()
  {
    // Arrange
    var inactiveUser = new User(
        "inactive@email.com",
        "fake-password-hash");

    inactiveUser.Deactivate(DateTime.UtcNow);

    var activeUser = new User(
        "active@email.com",
        "fake-password-hash");

    var repository = new FakeUserRepository(
        [inactiveUser, activeUser]);

    var handler = new GetInactiveUsersHandler(repository);

    // Act
    var result = await handler.Handle(CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    var response = Assert.Single(result.Value);

    Assert.Equal(inactiveUser.Id, response.Id);
    Assert.Equal("inactive@email.com", response.Email);
    Assert.False(response.Active);
    Assert.NotNull(response.DeletedAt);
    Assert.Equal(inactiveUser.DeletedAt, response.DeletedAt);
  }

  [Fact]
  public async Task Handle_WhenNoInactiveUsersExist_ReturnsEmptyList()
  {
    // Arrange
    var activeUser = new User(
        "active@email.com",
        "fake-password-hash");

    var repository = new FakeUserRepository([activeUser]);
    var handler = new GetInactiveUsersHandler(repository);

    // Act
    var result = await handler.Handle(CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Empty(result.Value);
  }

  private sealed class FakeUserRepository(
      IReadOnlyList<User> users) : IUserRepository
  {
    public Task<IReadOnlyList<User>> GetInactiveAsync(CancellationToken cancellationToken)
    {
      return Task.FromResult<IReadOnlyList<User>>(users.Where(user => !user.Active).ToList());
    }

    public Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken)
    {
      return Task.FromResult<IReadOnlyList<User>>(users.Where(user => user.Active).ToList());
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
    {
      return Task.FromResult(users);
    }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
      throw new NotSupportedException();
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
      throw new NotSupportedException();
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
      throw new NotSupportedException();
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }
  }
}
