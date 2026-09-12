
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

    public void ChangeEmail(string newEmail, DateTime utcNow)
    {
        Email = Email.Create(newEmail);
        UpdatedAt = utcNow;
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

    public void Reactivate(DateTime utcNow)
    {
        if (Active || DeletedAt is null)
        {
            return;
        }

        if (DeletedAt.Value.AddDays(15) < utcNow)
        {
            return;
        }

        Active = true;
        DeletedAt = null;
        UpdatedAt = utcNow;
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