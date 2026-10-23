using BuildingBlocks.Results;
using UserService.Application.Abstractions;

namespace UserService.Application.Users.GetAllUsers;

public sealed class GetAllUsersHandler(IUserRepository userRepository)
{
  public async Task<ResultOfT<IReadOnlyList<GetAllUsersResponse>>> Handle(
      CancellationToken cancellationToken)
  {
    var users = await userRepository.GetAllAsync(
        cancellationToken);

    IReadOnlyList<GetAllUsersResponse> response = users
        .Select(user => new GetAllUsersResponse(
            user.Id,
            user.Email.Value,
            user.CreatedAt,
            user.UpdatedAt,
            user.Active,
            user.DeletedAt))
        .ToList();

    return ResultOfT<IReadOnlyList<GetAllUsersResponse>>
        .Success(response);
  }
}