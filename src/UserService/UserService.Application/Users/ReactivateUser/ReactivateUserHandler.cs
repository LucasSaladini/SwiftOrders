using BuildingBlocks.Results;
using UserService.Application.Abstractions;

namespace UserService.Application.Users.ReactivateUser;

public sealed class ReactivateUserHandler(IUserRepository userRepository)
{
  public async Task<Result> Handle(Guid id, CancellationToken cancellationToken)
  {
    if (id == Guid.Empty)
    {
      return Result.Failure(new Error("User.InvalidId", "User ID must not be empty."));
    }

    var user = await userRepository.GetByIdAsync(id, cancellationToken);

    if (user is null)
    {
      return Result.Failure(new Error("User.NotFound", "User was not found."));
    }

    var result = user.Reactivate(DateTime.UtcNow);

    if (result.IsFailure)
    {
      return result;
    }

    await userRepository.UpdateAsync(user, cancellationToken);

    return Result.Success();
  }
}