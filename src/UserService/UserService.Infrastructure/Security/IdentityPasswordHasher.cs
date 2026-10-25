using Microsoft.AspNetCore.Identity;
using UserService.Application.Abstractions;

namespace UserService.Infrastructure.Security;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _passwordHasher = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return _passwordHasher.HashPassword(new object(), password);
    }

    public bool Verify(string passwordHash, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var result = _passwordHasher.VerifyHashedPassword(new object(), passwordHash, password);

        return result != PasswordVerificationResult.Failed;
    }
}