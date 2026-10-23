using UserService.Application.Abstractions;
using UserService.Application.Users.ReactivateUser;
using UserService.Domain.Entities;

namespace UserService.Tests.Application.Users.ReactivateUser;

public class ReactivateUserHandlerTests
{
  [Fact]
  public async Task Handle_WhenUserIsWithinReactivationWindow_ReactivatesAndUpdatesUser()
  {
    // Arrange
    var user = new User("user@email.com", "ValidPassword123!");
    var deactivatedAt = DateTime.UtcNow.AddDays(-5);
    user.Deactivate(deactivatedAt);

    var repository = new FakeUserRepository([user]);
    var handler = new ReactivateUserHandler(repository);

    // Act
    var result = await handler.Handle(user.Id, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.True(user.Active);
    Assert.Null(user.DeletedAt);
    Assert.True(user.UpdatedAt >= deactivatedAt);
    Assert.Equal(1, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenIdIsEmpty_ReturnsInvalidId()
  {
    // Arrange
    var repository = new FakeUserRepository([]);
    var handler = new ReactivateUserHandler(repository);

    // Act
    var result = await handler.Handle(Guid.Empty, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal("User.InvalidId", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
  {
    // Arrange
    var repository = new FakeUserRepository([]);
    var handler = new ReactivateUserHandler(repository);

    // Act
    var result = await handler.Handle(Guid.NewGuid(), CancellationToken.None);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal("User.NotFound", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenReactivationWindowExpired_ReturnsFailureWithoutUpdatingUser()
  {
    // Arrange
    var user = new User("user@email.com", "ValidPassword123!");
    var deactivatedAt = DateTime.UtcNow.AddDays(-16);
    user.Deactivate(deactivatedAt);

    var originalUpdatedAt = user.UpdatedAt;
    var repository = new FakeUserRepository([user]);
    var handler = new ReactivateUserHandler(repository);

    // Act
    var result = await handler.Handle(user.Id, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal("User.ReactivationWindowExpired", result.Error?.Code);
    Assert.False(user.Active);
    Assert.Equal(deactivatedAt, user.DeletedAt);
    Assert.Equal(originalUpdatedAt, user.UpdatedAt);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenUserIsAlreadyActive_ReturnsFailureWithoutUpdatingUser()
  {
    // Arrange
    var user = new User("user@email.com", "ValidPassword123!");
    var repository = new FakeUserRepository([user]);
    var handler = new ReactivateUserHandler(repository);

    // Act
    var result = await handler.Handle(user.Id, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Equal("User.AlreadyActive", result.Error?.Code);
    Assert.True(user.Active);
    Assert.Null(user.DeletedAt);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenCancellationIsRequested_ThrowsOperationCanceledException()
  {
    // Arrange
    var user = new User("user@email.com", "ValidPassword123!");
    var repository = new FakeUserRepository([user]);
    var handler = new ReactivateUserHandler(repository);

    using var cancellationTokenSource = new CancellationTokenSource();
    await cancellationTokenSource.CancelAsync();

    // Act & Assert
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.Handle(user.Id, cancellationTokenSource.Token));

    Assert.Equal(0, repository.UpdateCallCount);
  }

  private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
  {
    private readonly List<User> _users = [.. users];

    public int UpdateCallCount { get; private set; }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      return Task.FromResult(_users.FirstOrDefault(user => user.Id == id));
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
      UpdateCallCount++;

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