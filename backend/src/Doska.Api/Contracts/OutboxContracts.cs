using Doska.Application.Abstractions;

namespace Doska.Api.Contracts;

/// <summary>Письмо файлового ящика в списке администратора.</summary>
public sealed record OutboxMessageResponse(
    string FileName,
    long Size,
    DateTimeOffset ModifiedAt,
    string Recipient,
    string Subject,
    DateTimeOffset? SentAt)
{
    /// <summary>Создаёт ответ из элемента ящика.</summary>
    public static OutboxMessageResponse From(EmailOutboxMessage message)
        => new(
            message.FileName,
            message.Size,
            message.ModifiedAt,
            message.Recipient,
            message.Subject,
            message.SentAt);
}

/// <summary>Содержимое письма файлового ящика.</summary>
public sealed record OutboxMessageContentResponse(string Content)
{
    /// <summary>Создаёт ответ из текста письма.</summary>
    public static OutboxMessageContentResponse From(string content) => new(content);
}
