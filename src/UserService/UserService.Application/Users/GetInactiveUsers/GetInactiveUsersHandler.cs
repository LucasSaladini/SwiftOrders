using BuildingBlocks.Results;
using UserService.Application.Abstractions;

namespace UserService.Application.Users.GetInactiveUsers;

public sealed class GetInactiveUsersHandler(
    IUserRepository userRepository)
{
  public async Task<ResultOfT<IReadOnlyList<GetInactiveUsersResponse>>> Handle(
      CancellationToken cancellationToken)
  {
    var users = await userRepository.GetInactiveAsync(cancellationToken);

    IReadOnlyList<GetInactiveUsersResponse> response = users
        .Select(user => new GetInactiveUsersResponse(
            user.Id,
            user.Email.Value,
            user.CreatedAt,
            user.UpdatedAt,
            user.Active,
            user.DeletedAt))
        .ToList();

    return ResultOfT<IReadOnlyList<GetInactiveUsersResponse>>.Success(response);
  }
}