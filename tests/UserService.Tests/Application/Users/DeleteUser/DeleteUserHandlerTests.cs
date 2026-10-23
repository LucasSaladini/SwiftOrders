using UserService.Application.Abstractions;
using UserService.Application.Users.DeleteUser;
using UserService.Domain.Entities;

namespace UserService.Tests.Application.Users.DeleteUser;

public class DeleteUserHandlerTests
{
  [Fact]
  public async Task Handle_WhenUserExists_DeactivatesUser()
  {
    // Arrange
    var user = new User("user@email.com", "ValidPassword123!");
    var repository = new FakeUserRepository([user]);
    var handler = new DeleteUserHandler(repository);

    // Act
    var result = await handler.Handle(user.Id, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.False(user.Active);
    Assert.NotNull(user.DeletedAt);
    Assert.Equal(user.DeletedAt, user.UpdatedAt);
  }

  [Fact]
  public async Task Handle_WhenIdIsEmpty_ReturnsInvalidId()
  {
    // Arrange
    var repository = new FakeUserRepository([]);
    var handler = new DeleteUserHandler(repository);

    // Act
    var result = await handler.Handle(Guid.Empty, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal("User.InvalidId", result.Error?.Code);
  }

  [Fact]
  public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
  {
    // Arrange
    var repository = new FakeUserRepository([]);
    var handler = new DeleteUserHandler(repository);

    // Act
    var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal("User.NotFound", result.Error?.Code);
  }

  [Fact]
  public async Task Handle_WhenUserIsAlreadyInactive_KeepsOriginalDeletionDate()
  {
    // Arrange
    var user = new User("user@email.com", "ValidPassword123!");
    var originalDeletionDate = DateTime.UtcNow.AddDays(-2);

    user.Deactivate(originalDeletionDate);

    var repository = new FakeUserRepository([user]);
    var handler = new DeleteUserHandler(repository);

    // Act
    var result = await handler.Handle(user.Id, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.False(user.Active);
    Assert.Equal(originalDeletionDate, user.DeletedAt);
    Assert.Equal(originalDeletionDate, user.UpdatedAt);
  }

  private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
  {
    private readonly List<User> _users = [.. users];

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
      return Task.FromResult(_users.FirstOrDefault(user => user.Id == id));
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
      var index = _users.FindIndex(existingUser => existingUser.Id == user.Id);

      if (index >= 0)
      {
        _users[index] = user;
      }

      return Task.CompletedTask;
    }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
      throw new NotSupportedException();
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken)
    {
      throw new NotSupportedException();
    }

    public Task<IReadOnlyList<User>> GetInactiveAsync(CancellationToken cancellationToken)
    {
      throw new NotSupportedException();
    }
  }
}