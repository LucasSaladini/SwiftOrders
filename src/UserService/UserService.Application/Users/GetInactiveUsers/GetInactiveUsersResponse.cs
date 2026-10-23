namespace UserService.Application.Users.GetInactiveUsers;

public sealed record GetInactiveUsersResponse(
    Guid Id,
    string Email,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool Active,
    DateTime? DeletedAt
);