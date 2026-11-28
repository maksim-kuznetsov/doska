namespace Doska.Api.Infrastructure;

/// <summary>Настройки внешнего провайдера входа. Секрет не покидает сервер.</summary>
public abstract class ExternalProviderOptions
{
    /// <summary>Идентификатор OAuth-клиента.</summary>
    public string? ClientId { get; set; }

    /// <summary>Секрет OAuth-клиента.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Признак полной настройки провайдера.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

/// <summary>Настройки входа через Google.</summary>
public sealed class GoogleSignInOptions : ExternalProviderOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "Google";
}

/// <summary>Настройки входа через Яндекс ID.</summary>
public sealed class YandexSignInOptions : ExternalProviderOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "Yandex";
}
