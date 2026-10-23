using UserService.Domain.Entities;

namespace UserService.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetInactiveAsync(CancellationToken cancellationToken);
    Task UpdateAsync(User user, CancellationToken cancellationToken);
}