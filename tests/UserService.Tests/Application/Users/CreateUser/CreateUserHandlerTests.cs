
using BuildingBlocks.Results;
using UserService.Application.Abstractions;
using UserService.Application.Users.CreateUser;
using UserService.Domain.Entities;

namespace UserService.Tests.Application.Users.CreateUser;

//
// CreateUserHandlerTests
//
// ├── Handle_WhenEmailIsEmpty_ReturnsInvalidEmail
// ├── Handle_WhenEmailIsInvalid_ReturnsInvalidEmail
// ├── Handle_WhenPasswordIsTooShort_ReturnsInvalidPassword
// ├── Handle_WhenPasswordIsEmptyOrWhitespace_ReturnsInvalidPassword
// ├── Handle_WhenEmailAlreadyExists_ReturnsEmailAlreadyExists
// ├── Handle_WhenDataIsValid_CreatesUser
// ├── Handle_WhenEmailHasWhitespace_NormalizesEmailForLookup
// ├── Handle_WhenPasswordHasherFails_PropagatesException
// ├── Handle_WhenEmailLookupFails_PropagatesException
// └── Handle_WhenUserPersistenceFails_PropagatesException
//


public class CreateUserHandlerTests
{
    private static CreateUserHandler CreateHandler(FakeUserRepository? repository = null, FakePasswordHasher? passwordHasher = null)
    {
        return new CreateUserHandler(
            repository ?? new FakeUserRepository(),
            passwordHasher ?? new FakePasswordHasher()
        );
    }

    [Fact]
    public async Task Handle_WhenEmailIsEmpty_ReturnsInvalidEmail()
    {
        // Arrange
        var handler = CreateHandler();

        var command = new CreateUserCommand("", "ValidPassword123!");

        // Act
        var result = await handler.Handler(
            command,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidEmail", result.Error!.Code);

    }

    [Fact]
    public async Task Handle_WhenEmailIsInvalid_ReturnsInvalidEmail()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateUserCommand("invalidemail.com", "ValidPassword123!");

        // Act
        var result = await handler.Handler(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidEmail", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsTooShort_ReturnsInvalidPassword()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateUserCommand("user@email.com", "Short123!");

        // Act
        var result = await handler.Handler(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidPassword", result.Error!.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_WhenPasswordIsEmptyOrWhitespace_ReturnsInvalidPassword(string password)
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateUserCommand("user@email.com", password);

        // Act
        var result = await handler.Handler(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidPassword", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsEmailAlreadyExists()
    {
        // Arrange
        var repository = new FakeUserRepository
        {
            EmailExists = true
        };

        var passwordHasher = new FakePasswordHasher();

        var handler = CreateHandler(repository, passwordHasher);
        var command = new CreateUserCommand("user@email.com", "ValidPassword123!");

        // Act
        var result = await handler.Handler(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.EmailAlreadyExists", result.Error!.Code);

        Assert.Equal(0, passwordHasher.HashCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task Handle_WhenDataIsValid_CreatesUser()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var passwordHasher = new FakePasswordHasher();
        var handler = CreateHandler(repository, passwordHasher);
        var command = new CreateUserCommand("user@email.com", "ValidPassword123!");

        // Act
        var result = await handler.Handler(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        Assert.Equal(1, passwordHasher.HashCallCount);
        Assert.Equal(1, repository.AddCallCount);
        Assert.NotNull(repository.SavedUser);
        Assert.Equal(result.Value, repository.SavedUser.Id);
    }

    [Fact]
    public async Task Handle_WhenEmailHasWhitespace_NormalizesEmailForLookup()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var handler = CreateHandler(repository);
        var command = new CreateUserCommand(" User@Email.com ", "ValidPassword123!");

        // Act
        var result = await handler.Handler(command, CancellationToken.None);

        // Then
        Assert.True(result.IsSuccess);
        Assert.Equal("USER@EMAIL.COM", repository.QueriedEmail);
        Assert.Equal("User@Email.com", repository.SavedUser!.Email.Value);
    }

    [Fact]
    public async Task Handle_WhenPasswordHasherFails_PropagatesException()
    {
        // Arrange
        var repository = new FakeUserRepository();
        var passwordHasher = new FailingPasswordHash();
        var handler = new CreateUserHandler(repository, passwordHasher);
        
        var command = new CreateUserCommand("user@email.com", "ValidPassword123!");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handler(command, CancellationToken.None)
        );

        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task Handle_WhenEmailLookupFails_PropagatesException()
    {
        // Arrange
        var repository = new FailingLookupUserRepository();
        var passwordHasher = new FakePasswordHasher();
        var handler = new CreateUserHandler(repository, passwordHasher);

        var command = new CreateUserCommand("user@email.com", "ValidPassword123!");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handler(command, CancellationToken.None)
        );

        Assert.Equal(0, passwordHasher.HashCallCount);
    }

    [Fact]
    public async Task Handle_WhenUserPersistenceFails_PropagatesException()
    {
        // Arrange
        var repository = new FailingAddUserRepository();
        var passwordHasher = new FakePasswordHasher();
        var handler = new CreateUserHandler(repository, passwordHasher);

        var command = new CreateUserCommand("user@email.com", "ValidPassword123!");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handler(command, CancellationToken.None)
        );

        Assert.Equal(1, passwordHasher.HashCallCount);
        Assert.Equal(1, repository.AddCallCount);
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public int HashCallCount { get; set; }
    public string Hash(string password)
    {
        HashCallCount++;

        return "fake-password-hash";
    }
}

internal sealed class FailingPasswordHash : IPasswordHasher
{
    public string Hash(string password)
    {
        throw new InvalidOperationException("Hashing failed.");
    }
}

internal sealed class FailingLookupUserRepository : IUserRepository
{
    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Email lookup failed.");
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("AddAsync should not be called");
    }
}

internal sealed class FailingAddUserRepository : IUserRepository
{
    public int AddCallCount { get; private set; }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        AddCallCount++;

        throw new InvalidOperationException("User persistence failed");
    }
}

internal sealed class FakeUserRepository : IUserRepository
{
    public bool EmailExists { get; set; }
    public int AddCallCount { get; private set; }
    public User? SavedUser { get; private set; }
    public string? QueriedEmail { get; private set; }

    public Task<bool> ExistsByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        QueriedEmail = normalizedEmail;

        return Task.FromResult(EmailExists);
    }

    public Task AddAsync(
        User user,
        CancellationToken cancellationToken)
    {
        SavedUser = user;
        AddCallCount++;

        return Task.CompletedTask;
    }
}