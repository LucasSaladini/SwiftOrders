namespace UserService.Application.Users.GetAllUsers;

public sealed record GetAllUsersResponse(
    Guid Id,
    string Email,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool Active,
    DateTime? DeletedAt
);