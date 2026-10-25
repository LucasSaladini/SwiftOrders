using BuildingBlocks.Results;
using UserService.Application.Abstractions;

namespace UserService.Application.Users.ChangePassword;

public sealed class ChangePasswordHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
{
  public async Task<Result> Handle(ChangePasswordCommand changePasswordCommand, CancellationToken cancellationToken)
  {
    if (changePasswordCommand.UserId == Guid.Empty)
    {
      return Result.Failure(new Error("User.InvalidId", "User ID must not be empty."));
    }

    if (string.IsNullOrWhiteSpace(changePasswordCommand.CurrentPassword))
    {
      return Result.Failure(new Error("User.IncorrectCurrentPassword", "Current password is required."));
    }

    if (string.IsNullOrWhiteSpace(changePasswordCommand.NewPassword) || changePasswordCommand.NewPassword.Length < 15)
    {
      return Result.Failure(new Error("User.InvalidPassword", "Password must contain at least 15 characters."));
    }

    var user = await userRepository.GetByIdAsync(changePasswordCommand.UserId, cancellationToken);

    if (user is null)
    {
      return Result.Failure(new Error("User.NotFound", "User was not found."));
    }

    if (!user.Active)
    {
      return Result.Failure(new Error("User.Inactive", "Inactive users cannot change their password"));
    }

    if (!passwordHasher.Verify(user.PasswordHash, changePasswordCommand.CurrentPassword))
    {
      return Result.Failure(new Error("User.IncorrectCurrentPassword", "Current password is incorrect."));
    }

    if (passwordHasher.Verify(user.PasswordHash, changePasswordCommand.NewPassword))
    {
      return Result.Failure(new Error("User.PasswordUnchanged", "New password must be different from the current password."));
    }

    var newPasswordHash = passwordHasher.Hash(changePasswordCommand.NewPassword);
    var result = user.ChangePassword(newPasswordHash, DateTime.UtcNow);

    if (result.IsFailure)
    {
      return result;
    }

    await userRepository.UpdateAsync(user, cancellationToken);

    return Result.Success();
  } 
}