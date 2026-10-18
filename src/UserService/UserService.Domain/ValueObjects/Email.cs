using System.Net.Mail;

namespace UserService.Domain.ValueObjects;
public sealed class Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email is required.", nameof(value));
        }

        try
        {
            var mailAddress = new MailAddress(value);

            return new Email(mailAddress.Address.ToLowerInvariant());
        }
        catch (FormatException)
        {
            throw new ArgumentException("Email is invalid.", nameof(value));
        }
    }
}