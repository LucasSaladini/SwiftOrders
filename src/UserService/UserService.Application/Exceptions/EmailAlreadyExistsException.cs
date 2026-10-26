namespace UserService.Application.Exceptions;

/// <summary>
/// Indicates that an email address is already registered.
/// </summary>
public sealed class EmailAlreadyExistsException : Exception
{
  public EmailAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
  {
  }
}