
using UserService.Domain.ValueObjects;
using BuildingBlocks.Results;

namespace UserService.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool Active { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private User()
    {
    }

    public User(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        Id = Guid.NewGuid();
        Email = Email.Create(email);
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        Active = true;
    }

    public Result ChangeEmail(string newEmail, DateTime utcNow)
    {
        if (!Active)
        {
            return Result.Failure(
                new Error(
                    "User.Inactive",
                    "Inactive users cannot change their email."));
        }

        Email email;

        try
        {
            email = Email.Create(newEmail);
        }
        catch (ArgumentException)
        {
            return Result.Failure(
                new Error(
                    "User.InvalidEmail",
                    "Email is invalid."));
        }

        if (Email.Value == email.Value)
        {
            return Result.Failure(
                new Error(
                    "User.EmailUnchanged",
                    "New email must be different from the current email."));
        }

        Email = email;
        UpdatedAt = utcNow;

        return Result.Success();
    }

    public void Deactivate(DateTime utcNow)
    {
        if (!Active)
        {
            return;
        }

        Active = false;
        DeletedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public Result Reactivate(DateTime utcNow)
    {
        if (Active)
        {
            return Result.Failure(new Error("User.AlreadyActive", "User is already active."));
        }

        if (DeletedAt is null)
        {
            return Result.Failure(new Error("User.NotDeactivated", "User has no deactivation timestamp"));
        }

        if (DeletedAt.Value.AddDays(15) < utcNow)
        {
            return Result.Failure(new Error("User.ReactivationWindowExpired", "The user reactivation window has expired."));
        }

        Active = true;
        DeletedAt = null;
        UpdatedAt = utcNow;

        return Result.Success();
    }

    public Result ChangePassword(string newPasswordHash, DateTime utcNow)
    {
        if (!Active)
        {
            return Result.Failure(
                new Error(
                    "User.Inactive",
                    "Inactive users cannot change their password"
                )
            );
        }

        PasswordHash = newPasswordHash;
        UpdatedAt = utcNow;

        return Result.Success();
    }
}