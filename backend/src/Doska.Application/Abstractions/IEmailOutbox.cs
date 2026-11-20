namespace Doska.Application.Abstractions;

/// <summary>Чтение писем из файлового ящика (режим <c>Email:Mode=File</c>).</summary>
public interface IEmailOutbox
{
    /// <summary>Возвращает письма ящика от новых к старым.</summary>
    Task<IReadOnlyList<EmailOutboxMessage>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Возвращает текст письма по имени файла для чтения.</summary>
    Task<EmailOutboxReadResult> ReadAsync(string fileName, CancellationToken cancellationToken = default);
}

/// <summary>Краткие сведения о письме в файловом ящике.</summary>
/// <param name="FileName">Имя файла письма — ключ для чтения.</param>
/// <param name="Size">Размер файла в байтах.</param>
/// <param name="ModifiedAt">Время последнего изменения файла.</param>
/// <param name="Recipient">Получатель из заголовка <c>To</c> (пусто, если не удалось прочитать).</param>
/// <param name="Subject">Тема из заголовка <c>Subject</c> (пусто, если не удалось прочитать).</param>
/// <param name="SentAt">Дата письма из заголовка <c>Date</c> либо <c>null</c>.</param>
public sealed record EmailOutboxMessage(
    string FileName,
    long Size,
    DateTimeOffset ModifiedAt,
    string Recipient,
    string Subject,
    DateTimeOffset? SentAt);

/// <summary>Результат чтения письма из файлового ящика.</summary>
public enum EmailOutboxReadStatus
{
    /// <summary>Письмо прочитано.</summary>
    Ok,

    /// <summary>Имя файла недопустимо либо выходит за пределы каталога ящика.</summary>
    InvalidName,

    /// <summary>Файл письма не найден.</summary>
    NotFound,
}

/// <summary>Текст письма и причина, если чтение не удалось.</summary>
/// <param name="Status">Итог чтения.</param>
/// <param name="Text">Текст письма при <see cref="EmailOutboxReadStatus.Ok"/>, иначе пусто.</param>
public sealed record EmailOutboxReadResult(EmailOutboxReadStatus Status, string Text)
{
    /// <summary>Письмо успешно прочитано.</summary>
    public static EmailOutboxReadResult Ok(string text) => new(EmailOutboxReadStatus.Ok, text);

    /// <summary>Имя файла недопустимо.</summary>
    public static EmailOutboxReadResult InvalidName() => new(EmailOutboxReadStatus.InvalidName, string.Empty);

    /// <summary>Файл письма не найден.</summary>
    public static EmailOutboxReadResult NotFound() => new(EmailOutboxReadStatus.NotFound, string.Empty);
}
