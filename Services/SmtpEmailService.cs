using DriveX.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DriveX.Services;

// Sends plain-text email over SMTP. Replaces Spring's JavaMailSender.
public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;

    public SmtpEmailService(IOptions<SmtpSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            throw new InvalidOperationException(
                "Smtp:Host is not configured. Set the Smtp values with 'dotnet user-secrets set'.");
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(
            string.IsNullOrWhiteSpace(_settings.From) ? _settings.Username : _settings.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.Auto);
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
