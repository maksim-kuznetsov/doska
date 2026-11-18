using System.Net;
using System.Net.Mail;
using Doska.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Doska.Infrastructure.Email;

/// <summary>Отправка писем по SMTP. Включается при заданном <see cref="EmailOptions.Host"/>.</summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    /// <inheritdoc />
    public async Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage(_options.From, email, subject, body);
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseStartTls,
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
