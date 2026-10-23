namespace UserService.Application.Users.GetUserById;

public sealed record GetUserByIdResponse(
  Guid Id,
  string Email,
  bool Active,
  DateTime CreatedAt,
  DateTime UpdatedAt,
  DateTime? DeletedAt
);