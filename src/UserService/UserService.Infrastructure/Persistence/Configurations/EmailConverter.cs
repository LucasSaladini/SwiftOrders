using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UserService.Domain.ValueObjects;

namespace UserService.Infrastructure.Persistence.Configurations;

public sealed class EmailConverter() : ValueConverter<Email, string>(
  email => email.Value,
  value => Email.Create(value)
);