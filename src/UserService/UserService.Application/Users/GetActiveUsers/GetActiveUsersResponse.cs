
namespace UserService.Application.Users.GetActiveUsers;

public sealed record GetActiveUsersResponse(
    Guid Id,
    string Email,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool Active,
    DateTime? DeletedAt
);