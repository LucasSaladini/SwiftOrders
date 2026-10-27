
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Version).IsRequired().IsConcurrencyToken();

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(user => user.UpdatedAt)
            .IsRequired();

        builder.Property(user => user.Active)
            .IsRequired();

        builder.Property(user => user.DeletedAt);

        builder.Property(user => user.Email)
            .HasConversion(new EmailConverter())
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("IX_users_email");
    }
}
