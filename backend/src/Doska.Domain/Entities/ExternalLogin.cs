namespace Doska.Domain.Entities;

/// <summary>
/// Идентичность внешнего провайдера входа, привязанная к аккаунту.
/// Пара «провайдер + subject» уникальна; у аккаунта не более одной идентичности каждого провайдера.
/// </summary>
public class ExternalLogin : Entity
{
    /// <summary>Максимальная длина идентификатора провайдера.</summary>
    public const int MaxProviderLength = 32;

    /// <summary>Максимальная длина идентификатора пользователя у провайдера.</summary>
    public const int MaxSubjectLength = 255;

    /// <summary>Идентификатор аккаунта, которому принадлежит идентичность.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Идентификатор провайдера, например <c>google</c> или <c>yandex</c>.</summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>Стабильный идентификатор пользователя у провайдера.</summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>Аккаунт, которому принадлежит идентичность.</summary>
    public User? User { get; private set; }

    /// <summary>Создаёт пустую запись: используется EF Core при чтении из базы.</summary>
    private ExternalLogin()
    {
    }

    /// <summary>Создаёт привязку внешней идентичности к аккаунту.</summary>
    /// <param name="userId">Идентификатор аккаунта.</param>
    /// <param name="provider">Идентификатор провайдера.</param>
    /// <param name="subject">Идентификатор пользователя у провайдера.</param>
    /// <param name="now">Момент привязки.</param>
    public ExternalLogin(Guid userId, string provider, string subject, DateTimeOffset now)
        : base(now)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Идентификатор аккаунта не может быть пустым.", nameof(userId));
        }

        UserId = userId;
        Provider = NormalizeProvider(provider);
        Subject = NormalizeSubject(subject);
    }

    /// <summary>Приводит идентификатор провайдера к единому виду и проверяет его.</summary>
    /// <param name="provider">Идентификатор провайдера.</param>
    /// <exception cref="ArgumentException">Идентификатор пуст или слишком длинный.</exception>
    public static string NormalizeProvider(string? provider)
        => Normalize(provider, MaxProviderLength, "провайдера", nameof(provider))!.ToLowerInvariant();

    /// <summary>Приводит идентификатор пользователя у провайдера к единому виду и проверяет его.</summary>
    /// <param name="subject">Идентификатор пользователя у провайдера.</param>
    /// <exception cref="ArgumentException">Идентификатор пуст или слишком длинный.</exception>
    public static string NormalizeSubject(string? subject)
        => Normalize(subject, MaxSubjectLength, "пользователя", nameof(subject))!;

    private static string? Normalize(string? value, int maxLength, string role, string parameterName)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > maxLength)
        {
            throw new ArgumentException(
                $"Идентификатор {role} не может быть пустым и не должен превышать {maxLength} символов.",
                parameterName);
        }

        return trimmed;
    }
}
