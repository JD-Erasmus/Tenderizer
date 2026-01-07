using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Tenderizer.Services.Interfaces;
using Tenderizer.Services.Options;

namespace Tenderizer.Services.Implementations;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException("Recipient email is required.", nameof(to));
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromDisplayName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(to);

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.SmtpEnableSsl,
        };

        if (!string.IsNullOrWhiteSpace(_options.SmtpUser))
        {
            client.Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPass);
        }
        else
        {
            client.UseDefaultCredentials = true;
        }

        // SmtpClient doesn't support CancellationToken.
        await client.SendMailAsync(message);
    }
}
