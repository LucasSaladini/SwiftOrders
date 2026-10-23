
using Microsoft.EntityFrameworkCore;
using UserService.Application.Abstractions;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(UserDbContext dbContext) : IUserRepository
{
    public Task<bool> ExistsByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            user => user.Email == Domain.ValueObjects.Email.Create(normalizedEmail),
            cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users.AsNoTracking().Where(user => user.Active).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetInactiveAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users.AsNoTracking().Where(user => !user.Active).ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Update(user);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
