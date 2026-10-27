
namespace UserService.Application.Exceptions;

/// <summary>
/// Indicates that a user was modified by another operation before the current update completed.
/// </summary>
public sealed class UserConcurrencyException : Exception
{
  public UserConcurrencyException(string message, Exception innerException) : base(message, innerException)
  {
  }
}
