
using UserService.Application.Abstractions;
using UserService.Application.Users.ChangePassword;
using UserService.Domain.Entities;

namespace UserService.Tests.Application.Users.ChangePassword;

public class ChangePasswordHandlerTests
{
  private const string CurrentPassword = "CurrentPassword123!";
  private const string NewPassword = "NewPassword123456!";

  [Fact]
  public async Task Handle_WhenDataIsValid_ChangesPasswordAndUpdatesUser()
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user);
    var hasher = new FakePasswordHasher();
    var handler = new ChangePasswordHandler(repository, hasher);
    var result = await handler.Handle(new ChangePasswordCommand(user.Id, CurrentPassword, NewPassword), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(hasher.Hash(NewPassword), user.PasswordHash);
    Assert.Equal(1, repository.UpdateCallCount);
    Assert.True(hasher.Verify(user.PasswordHash, NewPassword));
  }

  [Fact]
  public async Task Handle_WhenIdIsEmpty_ReturnsInvalidId()
  {
    var repository = new FakeUserRepository();
    var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

    var result = await handler.Handle(new ChangePasswordCommand(Guid.Empty, CurrentPassword, NewPassword), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.InvalidId", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  public async Task Handle_WhenCurrentPasswordIsEmpty_ReturnsFailure(string password)
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user);
    var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

    var result = await handler.Handle(new ChangePasswordCommand(user.Id, password, NewPassword), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.IncorrectCurrentPassword", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Theory]
  [InlineData("")]
  [InlineData("Short123!")]
  public async Task Handle_WhenNewPasswordIsInvalid_ReturnsInvalidPassword(string password)
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user);
    var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

    var result = await handler.Handle(new ChangePasswordCommand(user.Id, CurrentPassword, password), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.InvalidPassword", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
  {
    var repository = new FakeUserRepository();
    var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

    var result = await handler.Handle(new ChangePasswordCommand(Guid.NewGuid(), CurrentPassword, NewPassword), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.NotFound", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenCurrentPasswordIsIncorrect_ReturnsFailure()
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user);
    var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

    var result = await handler.Handle(new ChangePasswordCommand(user.Id, "WrongPassword123!", NewPassword), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.IncorrectCurrentPassword", result.Error?.Code);
    Assert.Equal("hash:" + CurrentPassword, user.PasswordHash);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenNewPasswordMatchesCurrentPassword_ReturnsFailure()
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user);
    var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

    var result = await handler.Handle(new ChangePasswordCommand(user.Id, CurrentPassword, CurrentPassword), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.PasswordUnchanged", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenUserIsInactive_ReturnsFailureWithoutUpdating()
  {
    var user = CreateUser();
    user.Deactivate(DateTime.UtcNow);

    var repository = new FakeUserRepository(user);
    var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

    var result = await handler.Handle(new ChangePasswordCommand(user.Id, CurrentPassword, NewPassword), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.Inactive", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenCancellationIsRequested_ThrowsOperationCanceledException()
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user);
    var handler = new ChangePasswordHandler(repository, new FakePasswordHasher());

    using var cancellationTokenSource = new CancellationTokenSource();
    await cancellationTokenSource.CancelAsync();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.Handle(
            new ChangePasswordCommand(user.Id, CurrentPassword, NewPassword),
            cancellationTokenSource.Token));

    Assert.Equal(0, repository.UpdateCallCount);
  }

  private static User CreateUser() => new("user@email.com", "hash:" + CurrentPassword);

  private sealed class FakePasswordHasher : IPasswordHasher
  {
    public string Hash(string password) => "hash:" + password;

    public bool Verify(string passwordHash, string password) => passwordHash == Hash(password);
  }

  private sealed class FakeUserRepository(params User[] users) : IUserRepository
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
      cancellationToken.ThrowIfCancellationRequested();
      UpdateCallCount++;

      return Task.CompletedTask;
    }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task AddAsync(User user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<User>> GetInactiveAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
  }
}
