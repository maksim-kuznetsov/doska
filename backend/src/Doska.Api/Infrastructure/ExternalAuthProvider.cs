namespace Doska.Api.Infrastructure;

/// <summary>Описание внешнего провайдера входа для публичного списка и маршрутизации.</summary>
/// <param name="Id">Идентификатор провайдера, например <c>google</c> или <c>yandex</c>.</param>
/// <param name="DisplayName">Название для кнопки входа.</param>
/// <param name="Scheme">Схема аутентификации провайдера.</param>
/// <param name="IsConfigured">Признак наличия настроек: без них вход не запускается.</param>
public sealed record ExternalAuthProvider(
    string Id,
    string DisplayName,
    string Scheme,
    bool IsConfigured);

/// <summary>Реестр известных внешних провайдеров входа.</summary>
public interface IExternalAuthProviderRegistry
{
    /// <summary>Все известные провайдеры, включая ненастроенные.</summary>
    IReadOnlyList<ExternalAuthProvider> Providers { get; }

    /// <summary>Находит провайдера по идентификатору без учёта регистра или возвращает <c>null</c>.</summary>
    ExternalAuthProvider? Find(string? id);
}

/// <summary>Реестр провайдеров по списку, собранному при запуске приложения.</summary>
public sealed class ExternalAuthProviderRegistry : IExternalAuthProviderRegistry
{
    private readonly IReadOnlyList<ExternalAuthProvider> _providers;
    private readonly Dictionary<string, ExternalAuthProvider> _byId;

    /// <summary>Создаёт реестр из переданных провайдеров.</summary>
    public ExternalAuthProviderRegistry(IEnumerable<ExternalAuthProvider> providers)
    {
        _providers = providers.ToList();
        _byId = _providers.ToDictionary(provider => provider.Id, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyList<ExternalAuthProvider> Providers => _providers;

    /// <inheritdoc />
    public ExternalAuthProvider? Find(string? id)
        => !string.IsNullOrWhiteSpace(id) && _byId.TryGetValue(id.Trim(), out var provider)
            ? provider
            : null;
}
