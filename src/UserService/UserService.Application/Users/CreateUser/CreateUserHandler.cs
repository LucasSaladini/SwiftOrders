using BuildingBlocks.Results;
using UserService.Application.Abstractions;
using UserService.Domain.Entities;
using UserService.Domain.ValueObjects;

namespace UserService.Application.Users.CreateUser;

public sealed class CreateUserHandler(
    IUserRepository userRepository, 
    IPasswordHasher passwordHasher)
{
    public async Task<ResultOfT<Guid>> Handler(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return ResultOfT<Guid>.Failure(new Error("User.InvalidEmail", "Email is required."));
        }

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 15)
        {
            return ResultOfT<Guid>.Failure(new Error("User.InvalidPassword", "Password must contain at least 15 characters."));
        }

        var email = command.Email.Trim();
        Email emailValue;

        try
        {
            emailValue = Email.Create(email);
        }
        catch (ArgumentException)
        {
            return ResultOfT<Guid>.Failure(new Error("User.InvalidEmail", "Email is invalid"));
        }

        var normalizedEmail = emailValue.Value;
        var emailExists = await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return ResultOfT<Guid>.Failure(new Error("User.EmailAlreadyExists", "An account with this email already exists"));
        }

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = new User(emailValue.Value, passwordHash);

        await userRepository.AddAsync(user, cancellationToken);

        return ResultOfT<Guid>.Success(user.Id);
    }
}