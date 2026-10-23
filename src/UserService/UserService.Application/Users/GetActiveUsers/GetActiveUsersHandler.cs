
using BuildingBlocks.Results;
using UserService.Application.Abstractions;

namespace UserService.Application.Users.GetActiveUsers;

public sealed class GetActiveUsersHandler(IUserRepository userRepository)
{
    public async Task<ResultOfT<IReadOnlyList<GetActiveUsersResponse>>> Handle(
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetActiveAsync(cancellationToken);

        IReadOnlyList<GetActiveUsersResponse> response = users
            .Select(user => new GetActiveUsersResponse(
                user.Id,
                user.Email.Value,
                user.CreatedAt,
                user.UpdatedAt,
                user.Active,
                user.DeletedAt))
            .ToList();

        return ResultOfT<IReadOnlyList<GetActiveUsersResponse>>.Success(response);
    }
}
