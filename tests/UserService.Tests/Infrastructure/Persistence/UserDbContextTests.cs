using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Persistence.Configurations;

namespace UserService.Tests.Infrastructure.Persistence;

public sealed class UserDbContextTests
{
  [Fact]
  public void Model_ShouldConfigureUserEntityCorrectlu()
  {
    var options = new DbContextOptionsBuilder<UserDbContext>()
                        .UseNpgsql("Host=localhost;Database=swiftorders_test;Username=text;Password=test")
                        .Options;

    using var context = new UserDbContext(options);
    var entity = context.Model.FindEntityType(typeof(User));

    Assert.NotNull(entity);
    Assert.Equal("users", entity.GetTableName());

    var primaryKey = entity.FindPrimaryKey();

    Assert.NotNull(primaryKey);
    Assert.Contains(primaryKey.Properties, property => property.Name == nameof(User.Id));
  }


  [Fact]
  public void Model_ShouldConfigureEmailConversion()
  {
    var options = new DbContextOptionsBuilder<UserDbContext>()
        .UseNpgsql("Host=localhost;Database=swiftorders_test;Username=test;Password=test")
        .Options;

    using var context = new UserDbContext(options);

    var entity = context.Model.FindEntityType(typeof(User));

    Assert.NotNull(entity);

    var emailProperty = entity.FindProperty(nameof(User.Email));

    Assert.NotNull(emailProperty);

    var converter = emailProperty.GetValueConverter();

    Assert.NotNull(converter);
    Assert.Equal(typeof(string), converter.ProviderClrType);
  }

  [Fact]
  public void Model_ShouldConfigureUserPropertiesCorrectly()
  {
    var options = new DbContextOptionsBuilder<UserDbContext>()
        .UseNpgsql("Host=localhost;Database=swiftorders_test;Username=test;Password=test")
        .Options;

    using var context = new UserDbContext(options);
    var entity = context.Model.FindEntityType(typeof(User));

    Assert.NotNull(entity);

    var email = entity.FindProperty(nameof(User.Email));
    var passwordHash = entity.FindProperty(nameof(User.PasswordHash));
    var createdAt = entity.FindProperty(nameof(User.CreatedAt));
    var updatedAt = entity.FindProperty(nameof(User.UpdatedAt));
    var active = entity.FindProperty(nameof(User.Active));

    Assert.NotNull(email);
    Assert.True(email.IsNullable == false);
    Assert.Equal(320, email.GetMaxLength());

    Assert.NotNull(passwordHash);
    Assert.False(passwordHash.IsNullable);
    Assert.Equal(500, passwordHash.GetMaxLength());

    Assert.NotNull(createdAt);
    Assert.False(createdAt.IsNullable);

    Assert.NotNull(updatedAt);
    Assert.False(updatedAt.IsNullable);

    Assert.NotNull(active);
    Assert.False(active.IsNullable);
  }
}