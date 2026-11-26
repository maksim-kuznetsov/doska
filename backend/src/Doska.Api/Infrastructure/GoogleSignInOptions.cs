namespace Doska.Api.Infrastructure;

/// <summary>Настройки входа через Google. Секрет не покидает сервер.</summary>
public sealed class GoogleSignInOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "Google";

    /// <summary>Идентификатор OAuth-клиента Google.</summary>
    public string? ClientId { get; set; }

    /// <summary>Секрет OAuth-клиента Google.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Признак полной настройки провайдера.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}