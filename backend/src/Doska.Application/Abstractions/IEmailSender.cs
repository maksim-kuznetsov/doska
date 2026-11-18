namespace Doska.Application.Abstractions;

/// <summary>Отправка писем. Реализация выбирается окружением.</summary>
public interface IEmailSender
{
    /// <summary>Отправляет письмо на адрес.</summary>
    Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default);
}
