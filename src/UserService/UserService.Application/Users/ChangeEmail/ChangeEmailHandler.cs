using BuildingBlocks.Results;
using UserService.Application.Abstractions;
using UserService.Domain.ValueObjects;

namespace UserService.Application.Users.ChangeEmail;

public sealed class ChangeEmailHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
{
  public async Task<Result> Handle(ChangeEmailCommand command, CancellationToken cancellationToken)
  {
    if (command.UserId == Guid.Empty)
    {
      return Result.Failure(new Error("User.InvalidId", "User ID must not be empty."));
    }

    if (string.IsNullOrWhiteSpace(command.CurrentPassword))
    {
      return Result.Failure(new Error("User.CurrentPasswordIsEmpty", "Current password is required"));
    }

    Email newEmail;

    try
    {
      newEmail = Email.Create(command.NewEmail);
    }
    catch (ArgumentException)
    {
      return Result.Failure(new Error("User.InvalidEmail", "Email is invalid"));
    }

    var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

    if (user is null)
    {
      return Result.Failure(new Error("User.NotFound", "User was not found."));
    }

    if (!user.Active)
    {
      return Result.Failure(new Error("User.Inactive", "Inactive users cannot change their email."));
    }

    if (!passwordHasher.Verify(user.PasswordHash, command.CurrentPassword))
    {
      return Result.Failure(new Error("User.IncorrectCurrentPassword", "Current password is incorrect"));
    }

    if (user.Email.Value == newEmail.Value)
    {
      return Result.Failure(new Error("User.EmailUnchanged", "New email must be different from the current email."));
    }

    var emailAlreadyExists = await userRepository.ExistsByEmailAsync(newEmail.Value, cancellationToken);

    if (emailAlreadyExists)
    {
      return Result.Failure(new Error("User.EmailAlreadyExists", "Email is already in use"));
    }

    var result = user.ChangeEmail(newEmail.Value, DateTime.UtcNow);

    if (result.IsFailure)
    {
      return result;
    }

    await userRepository.UpdateAsync(user, cancellationToken);

    return Result.Success();
  }
}