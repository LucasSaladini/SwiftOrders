namespace UserService.Application.Users.ChangeEmail;

public sealed record ChangeEmailCommand(Guid UserId, string CurrentPassword, string NewEmail);