using UserService.Domain.Entities;

namespace UserService.Tests.Domain.Entities;

// UserTests
//
// ├── Constructor_WhenValidEmail_CreatesUser
// ├── Constructor_GeneratesId
// ├── Constructor_CreatesUserAsActive
// ├── Constructor_SetsCreatedAt
// ├── Constructor_SetsUpdatedAtEqualToCreatedAt
// ├── ChangeEmail_UpdatesEmail
// ├── ChangeEmail_UpdatesUpdatedAt
// ├── Deactivate_DeactivatesUser
// ├── Deactivate_SetsDeletedAt
// ├── Deactivate_UpdatesUpdatedAt
// ├── Deactivate_WhenUserIsAlreadyInactive_DoesNotChangeDeletedAt
// ├── Reactivate_WhenWithin15Days_ReactivatesUser
// └── Reactivate_WhenMoreThan15DaysHavePassed_DoesNotReactivateUser

public class UserTests
{
    [Fact]
    public void Constructor_WhenValidEmail_CreatesUser()
    {
        // Arrange
        var email = "user@example.com";
        var passwordHash = "new-password-hash";

        // Act
        var user = new User(email, passwordHash);

        // Assert
        Assert.Equal(email, user.Email.Value);
    }

    [Fact]
    public void Constructor_GeneratesId()
    {
        // Arrange
        var email = "user@example.com";
        var passwordHash = "new-password-hash";

        // Act
        var user = new User(email, passwordHash);

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Constructor_CreatesUserAsActive()
    {
        // Arrange
        var email = "user@example.com";
        var passwordHash = "new-password-hash";

        // Act
        var user = new User(email, passwordHash);

        // Assert
        Assert.True(user.Active);
    }

    [Fact]
    public void Constructor_SetsCreatedAt()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var user = new User("user@example.com", "password-hash");

        // Assert
        var after = DateTime.UtcNow;

        Assert.InRange(user.CreatedAt, before, after);
    }

    [Fact]
    public void Constructor_SetsUpdatedAtEqualToCreatedAt()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");

        // Act & Assert
        Assert.Equal(user.CreatedAt, user.UpdatedAt);
    }

    [Fact]
    public void Constructor_SetsPasswordHash()
    {
        // Arrange
        var passwordHash = "password-hash";

        // Act
        var user = new User("user@example.com", passwordHash);

        // Assert
        Assert.Equal(passwordHash, user.PasswordHash);
    }

    [Fact]
    public void ChangeEmail_UpdatesEmail()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var changedAt = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var newEmail = "new@example.com";

        // Act
        user.ChangeEmail(newEmail, changedAt);

        // Assert
        Assert.Equal(newEmail, user.Email.Value);
    }

    [Fact]
    public void Deactivate_DeactivatesUser()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var now = DateTime.UtcNow;

        // Act
        user.Deactivate(now);

        // Assert
        Assert.False(user.Active);
    }

    [Fact]
    public void Deactivate_SetsDeletedAt()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var now = DateTime.UtcNow;

        // Act
        user.Deactivate(now);

        // Assert
        Assert.Equal(now, user.DeletedAt);
    }

    [Fact]
    public void Deactivate_UpdatesUpdatedAt()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var now = DateTime.UtcNow;

        // Act
        user.Deactivate(now);

        // Assert
        Assert.Equal(now, user.UpdatedAt);
    }

    [Fact]
    public void Deactivate_WhenUserIsAlreadyInactive_DoesNotChangeDeletedAt()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var firstDeactivation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        user.Deactivate(firstDeactivation);

        var deletedAt = user.DeletedAt;
        var updatedAt = user.UpdatedAt;

        // Act
        var secondDeactivation = firstDeactivation.AddHours(1);

        user.Deactivate(secondDeactivation);

        // Assert
        Assert.Equal(deletedAt, user.DeletedAt);
        Assert.Equal(updatedAt, user.UpdatedAt);
    }
    [Fact]
    public void Reactivate_WhenWithin15Days_ReactivatesUser()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var deactivatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var reactivationTime = deactivatedAt.AddDays(10);

        user.Deactivate(deactivatedAt);

        // Act
        var result = user.Reactivate(reactivationTime);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.True(user.Active);
        Assert.Null(user.DeletedAt);
        Assert.Equal(reactivationTime, user.UpdatedAt);
    }

    [Fact]
    public void Reactivate_WhenExactly15DaysHavePassed_ReactivatesUser()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var deactivatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var reactivationTime = deactivatedAt.AddDays(15);

        user.Deactivate(deactivatedAt);

        // Act
        var result = user.Reactivate(reactivationTime);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.True(user.Active);
        Assert.Null(user.DeletedAt);
        Assert.Equal(reactivationTime, user.UpdatedAt);
    }

    [Fact]
    public void Reactivate_WhenMoreThan15DaysHavePassed_DoesNotReactivateUser()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var deactivatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var reactivationTime = deactivatedAt.AddDays(16);

        user.Deactivate(deactivatedAt);
        var originalUpdatedAt = user.UpdatedAt;

        // Act
        var result = user.Reactivate(reactivationTime);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("User.ReactivationWindowExpired", result.Error.Code);
        Assert.False(user.Active);
        Assert.Equal(deactivatedAt, user.DeletedAt);
        Assert.Equal(originalUpdatedAt, user.UpdatedAt);
    }

    [Fact]
    public void ChangeEmail_UpdatesUpdatedAt()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");

        var changedAt = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);

        // Act
        user.ChangeEmail("new@example.com", changedAt);

        // Assert
        Assert.Equal(changedAt, user.UpdatedAt);
    }

    [Fact]
    public void ChangePassword_UpdatesPasswordHash()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var newPasswordHash = "new-password-hash";
        var changedAt = new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = user.ChangePassword(newPasswordHash, changedAt);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newPasswordHash, user.PasswordHash);
    }

    [Fact]
    public void ChangePassword_UpdatesUpdatedAt()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var newPasswordHash = "new-password-hash";
        var changedAt = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = user.ChangePassword(newPasswordHash, changedAt);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(changedAt, user.UpdatedAt);
    }

    [Fact]
    public void ChangePassword_WhenUserIsInactive_ReturnsFailure()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var deactivatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        user.Deactivate(deactivatedAt);

        var changedAt = deactivatedAt.AddDays(1);
        var originalPasswordHash = user.PasswordHash;

        // Act
        var result = user.ChangePassword("new-password-hash", changedAt);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(originalPasswordHash, user.PasswordHash);
        Assert.Equal(deactivatedAt, user.UpdatedAt);
    }

    [Fact]
    public void Reactivate_UpdatesUpdatedAt()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");

        var deactivatedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var reactivationTime = deactivatedAt.AddDays(5);

        user.Deactivate(deactivatedAt);

        // Act
        var result = user.Reactivate(reactivationTime);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(reactivationTime, user.UpdatedAt);
    }

    [Fact]
    public void Reactivate_WhenUserIsAlreadyActive_ReturnsFailure()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");
        var originalUpdatedAt = user.UpdatedAt;
        var reactivationTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = user.Reactivate(reactivationTime);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("User.AlreadyActive", result.Error.Code);

        Assert.True(user.Active);
        Assert.Null(user.DeletedAt);
        Assert.Equal(originalUpdatedAt, user.UpdatedAt);
    }

    [Fact]
    public void Reactivate_WhenExpired_DoesNotChangeUpdatedAt()
    {
        // Arrange
        var user = new User("user@example.com", "password-hash");

        var deactivatedAt = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var expiredReactivationTime = deactivatedAt.AddDays(16);

        user.Deactivate(deactivatedAt);
        var originalUpdatedAt = user.UpdatedAt;

        // Act
        var result = user.Reactivate(expiredReactivationTime);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("User.ReactivationWindowExpired", result.Error.Code);

        Assert.False(user.Active);
        Assert.Equal(deactivatedAt, user.DeletedAt);
        Assert.Equal(originalUpdatedAt, user.UpdatedAt);
    }

    [Fact]
    public void Constructor_WhenPasswordHashIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var passwordHash = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new User("user@example.com", passwordHash));
    }
}