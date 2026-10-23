
using BuildingBlocks.Results;
using UserService.Application.Abstractions;

namespace UserService.Application.Users.GetUserById;

public sealed class GetUserByIdHandler(IUserRepository userRepository)
{
  public async Task<ResultOfT<GetUserByIdResponse>> Handle(
      Guid id,
      CancellationToken cancellationToken)
  {
    if (id == Guid.Empty)
    {
      return ResultOfT<GetUserByIdResponse>.Failure(
          new Error(
              "User.InvalidId",
              "User ID must not be empty."));
    }

    var user = await userRepository.GetByIdAsync(
        id,
        cancellationToken);

    if (user is null)
    {
      return ResultOfT<GetUserByIdResponse>.Failure(
          new Error(
              "User.NotFound",
              "User was not found."));
    }

    var response = new GetUserByIdResponse(
        user.Id,
        user.Email.Value,
        user.Active,
        user.CreatedAt,
        user.UpdatedAt,
        user.DeletedAt);

    return ResultOfT<GetUserByIdResponse>.Success(response);
  }
}
