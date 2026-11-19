namespace Doska.Infrastructure.Email;

/// <summary>Настройки отправки писем.</summary>
public sealed class EmailOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "Email";

    /// <summary>
    /// Режим отправки: <see cref="EmailModes.Log"/>, <see cref="EmailModes.File"/> или
    /// <see cref="EmailModes.Smtp"/>. Пусто — режим выводится из <see cref="Host"/>.
    /// </summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>Каталог файлового ящика для режима <see cref="EmailModes.File"/>.</summary>
    public string OutboxPath { get; set; } = "media/outbox";

    /// <summary>Хост SMTP. Пусто — письма пишутся в журнал.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Порт SMTP.</summary>
    public int Port { get; set; } = 587;

    /// <summary>Логин SMTP.</summary>
    public string? UserName { get; set; }

    /// <summary>Пароль SMTP.</summary>
    public string? Password { get; set; }

    /// <summary>Адрес отправителя.</summary>
    public string From { get; set; } = "no-reply@doska.local";

    /// <summary>Использовать TLS.</summary>
    public bool UseStartTls { get; set; } = true;
}

/// <summary>Режимы отправки писем.</summary>
public static class EmailModes
{
    /// <summary>Писать письма в журнал приложения.</summary>
    public const string Log = "Log";

    /// <summary>Сохранять письма как файлы в каталоге исходящих.</summary>
    public const string File = "File";

    /// <summary>Отправлять письма по SMTP.</summary>
    public const string Smtp = "Smtp";
}
