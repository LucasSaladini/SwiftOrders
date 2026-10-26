
using UserService.Application.Abstractions;
using UserService.Application.Users.ChangeEmail;
using UserService.Domain.Entities;

namespace UserService.Tests.Application.Users.ChangeEmail;

public class ChangeEmailHandlerTests
{
  private const string CurrentPassword = "CurrentPassword123!";

  [Fact]
  public async Task Handle_WhenDataIsValid_ChangesEmailAndUpdatesUser()
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user);
    var handler = CreateHandler(repository);

    var result = await handler.Handle(new ChangeEmailCommand(user.Id, CurrentPassword, "new@example.com"), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal("new@example.com", user.Email.Value);
    Assert.Equal(1, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenIdIsEmpty_ReturnsInvalidId()
  {
    var repository = new FakeUserRepository();
    var handler = CreateHandler(repository);

    var result = await handler.Handle(new ChangeEmailCommand(Guid.Empty, CurrentPassword, "new@example.com"), CancellationToken.None);

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
    var handler = CreateHandler(repository);

    var result = await handler.Handle(new ChangeEmailCommand(user.Id, password, "new@example.com"), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.CurrentPasswordIsEmpty", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenEmailIsInvalid_ReturnsInvalidEmail()
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user);
    var handler = CreateHandler(repository);

    var result = await handler.Handle(new ChangeEmailCommand(user.Id, CurrentPassword, "invalid-email"), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.InvalidEmail", result.Error?.Code);
    Assert.Equal("user@example.com", user.Email.Value);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenUserDoesNotExist_ReturnsNotFound()
  {
    var repository = new FakeUserRepository();
    var handler = CreateHandler(repository);

    var result = await handler.Handle(new ChangeEmailCommand(Guid.NewGuid(), CurrentPassword, "new@example.com"), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.NotFound", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenUserIsInactive_ReturnsFailureWithoutUpdating()
  {
    var user = CreateUser();
    user.Deactivate(DateTime.UtcNow);

    var repository = new FakeUserRepository(user);
    var handler = CreateHandler(repository);

    var result = await handler.Handle(new ChangeEmailCommand(user.Id, CurrentPassword, "new@example.com"), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.Inactive", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenCurrentPasswordIsIncorrect_ReturnsFailure()
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user);
    var handler = CreateHandler(repository);

    var result = await handler.Handle(new ChangeEmailCommand(user.Id, "WrongPassword123!", "new@example.com"), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.IncorrectCurrentPassword", result.Error?.Code);
    Assert.Equal("user@example.com", user.Email.Value);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenEmailAlreadyExists_ReturnsFailure()
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user)
    {
      EmailAlreadyExists = true
    };
    var handler = CreateHandler(repository);

    var result = await handler.Handle(new ChangeEmailCommand(user.Id, CurrentPassword, "existing@example.com"), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.EmailAlreadyExists", result.Error?.Code);
    Assert.Equal("user@example.com", user.Email.Value);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenEmailIsUnchanged_ReturnsFailure()
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user);
    var handler = CreateHandler(repository);

    var result = await handler.Handle(new ChangeEmailCommand(user.Id, CurrentPassword, "USER@EXAMPLE.COM"), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("User.EmailUnchanged", result.Error?.Code);
    Assert.Equal(0, repository.UpdateCallCount);
  }

  [Fact]
  public async Task Handle_WhenCancellationIsRequested_ThrowsOperationCanceledException()
  {
    var user = CreateUser();
    var repository = new FakeUserRepository(user);
    var handler = CreateHandler(repository);

    using var cancellationTokenSource = new CancellationTokenSource();
    await cancellationTokenSource.CancelAsync();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
      handler.Handle(new ChangeEmailCommand(user.Id, CurrentPassword, "new@example.com"), cancellationTokenSource.Token));

    Assert.Equal(0, repository.UpdateCallCount);
  }

  private static User CreateUser() => new("user@example.com", "hash:" + CurrentPassword);

  private static ChangeEmailHandler CreateHandler(FakeUserRepository repository) => new(repository, new FakePasswordHasher());

  private sealed class FakePasswordHasher : IPasswordHasher
  {
    public string Hash(string password) => "hash:" + password;

    public bool Verify(string passwordHash, string password) => passwordHash == Hash(password);
  }

  private sealed class FakeUserRepository(params User[] users) : IUserRepository
  {
    private readonly List<User> _users = [.. users];

    public int UpdateCallCount { get; private set; }

    public bool EmailAlreadyExists { get; init; }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      return Task.FromResult(_users.FirstOrDefault(user => user.Id == id));
    }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      return Task.FromResult(EmailAlreadyExists);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      UpdateCallCount++;

      return Task.CompletedTask;
    }

    public Task AddAsync(User user, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<User>> GetInactiveAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
  }
}
