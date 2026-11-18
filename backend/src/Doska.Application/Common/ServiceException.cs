namespace Doska.Application.Common;

/// <summary>Категория ошибки сценария, определяющая HTTP-код ответа.</summary>
public enum ServiceErrorKind
{
    /// <summary>Некорректные данные — 400.</summary>
    Validation = 0,

    /// <summary>Нет аутентификации — 401.</summary>
    Unauthorized = 1,

    /// <summary>Недостаточно прав — 403.</summary>
    Forbidden = 2,

    /// <summary>Объект не найден — 404.</summary>
    NotFound = 3,

    /// <summary>Конфликт состояния — 409.</summary>
    Conflict = 4,
}

/// <summary>
/// Ошибка сценария приложения с категорией для отображения в формате
/// <c>ProblemDetails</c>.
/// </summary>
public sealed class ServiceException : Exception
{
    /// <summary>Создаёт ошибку сценария.</summary>
    public ServiceException(ServiceErrorKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }

    /// <summary>Категория ошибки.</summary>
    public ServiceErrorKind Kind { get; }
}
