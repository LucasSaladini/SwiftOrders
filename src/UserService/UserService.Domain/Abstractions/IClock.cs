namespace UserService.Domain.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}