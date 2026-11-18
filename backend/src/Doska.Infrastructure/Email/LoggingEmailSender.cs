using Doska.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Doska.Infrastructure.Email;

/// <summary>
/// Отправка писем в журнал: используется в разработке и тестах, когда SMTP не настроен.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    public Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Письмо для {Email}. Тема: {Subject}. Текст: {Body}", email, subject, body);
        return Task.CompletedTask;
    }
}
