namespace Doska.Infrastructure.Email;

/// <summary>Настройки отправки писем.</summary>
public sealed class EmailOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "Email";

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
