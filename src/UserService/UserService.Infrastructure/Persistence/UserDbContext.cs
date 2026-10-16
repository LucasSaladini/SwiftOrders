using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Infrastructure.Persistence.Configurations;

namespace UserService.Infrastructure.Persistence;

public sealed class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
{
  public DbSet<User> Users => Set<User>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyConfiguration(new UserConfiguration());
  }
}
