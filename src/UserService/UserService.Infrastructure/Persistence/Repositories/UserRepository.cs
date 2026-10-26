
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UserService.Application.Abstractions;
using UserService.Application.Exceptions;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(UserDbContext dbContext) : IUserRepository
{
    private const string EmailUniqueIndex = "IX_users_email";

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(user => user.Email == Domain.ValueObjects.Email.Create(normalizedEmail), cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
        await SaveChangesAsync(cancellationToken);
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
        await SaveChangesAsync(cancellationToken);
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsEmailUniqueConstraintViolation(exception))
        {
            throw new EmailAlreadyExistsException("Email is already in use.", exception);
        }
    }

    private static bool IsEmailUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
            && postgresException.ConstraintName == EmailUniqueIndex;
    }
}
