using System.Collections.Concurrent;
using Doska.Application.Abstractions;

namespace Doska.Tests;

/// <summary>Запоминает письма вместо реальной отправки: позволяет проверять адресата и тему.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _messages = new();

    /// <summary>Отправленные письма.</summary>
    public IReadOnlyCollection<SentEmail> Messages => _messages.ToArray();

    /// <inheritdoc />
    public Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(new SentEmail(email, subject, body));
        return Task.CompletedTask;
    }

    /// <summary>Очищает собранные письма.</summary>
    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }

    /// <summary>Отправленное письмо.</summary>
    public sealed record SentEmail(string Email, string Subject, string Body);
}
