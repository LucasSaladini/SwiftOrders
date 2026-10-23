using Microsoft.AspNetCore.Identity;
using UserService.Infrastructure.Security;
using Xunit;

namespace UserService.Tests.Infrastructure.Security;

public sealed class IdentityPasswordHasherTests
{
    [Fact]
    public void Hash_ShouldReturnDifferentHashesForSamePassword()
    {
        // Arrange
        var sut = new IdentityPasswordHasher();
        var verifier = new PasswordHasher<object>();
        var user = new object();
        const string password = "A-Strong-Test-Password-123!";

        // Act
        var firstHash = sut.Hash(password);
        var secondHash = sut.Hash(password);

        // Assert
        Assert.NotEqual(password, firstHash);
        Assert.NotEqual(password, secondHash);
        Assert.NotEqual(firstHash, secondHash);

        Assert.Equal(
            PasswordVerificationResult.Success,
            verifier.VerifyHashedPassword(user, firstHash, password));

        Assert.Equal(
            PasswordVerificationResult.Success,
            verifier.VerifyHashedPassword(user, secondHash, password));
    }
}