
using UserService.Application.Abstractions;
using UserService.Application.Users.GetAllUsers;
using UserService.Domain.Entities;

namespace UserService.Tests.Application.Users.GetAllUsers;

public class GetAllUsersHandlerTests
{
  [Fact]
  public async Task Handle_WhenUsersExist_ReturnsAllUsers()
  {
    // Arrange
    var firstUser = new User(
        "user1@email.com",
        "fake-password-hash");

    var secondUser = new User(
        "user2@email.com",
        "fake-password-hash");

    var repository = new FakeUserRepository(
        [firstUser, secondUser]);

    var handler = new GetAllUsersHandler(repository);

    // Act
    var result = await handler.Handle(CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal(2, result.Value.Count);

    var firstResponse = Assert.Single(result.Value, user => user.Id == firstUser.Id);

    Assert.Equal("user1@email.com", firstResponse.Email);
    Assert.Equal(firstUser.CreatedAt, firstResponse.CreatedAt);
    Assert.Equal(firstUser.UpdatedAt, firstResponse.UpdatedAt);
    Assert.True(firstResponse.Active);
    Assert.Null(firstResponse.DeletedAt);

    var secondResponse = Assert.Single(result.Value, user => user.Id == secondUser.Id);

    Assert.Equal("user2@email.com", secondResponse.Email);
  }

  [Fact]
  public async Task Handle_WhenNoUsersExist_ReturnsEmptyList()
  {
    // Arrange
    var repository = new FakeUserRepository([]);
    var handler = new GetAllUsersHandler(repository);

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

    public Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken)
    {
      return Task.FromResult<IReadOnlyList<User>>(users.Where(user => user.Active).ToList());
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
