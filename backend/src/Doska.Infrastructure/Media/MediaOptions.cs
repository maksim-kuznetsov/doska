namespace Doska.Infrastructure.Media;

/// <summary>Настройки хранения и отдачи медиафайлов.</summary>
public sealed class MediaOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "Media";

    /// <summary>Корневой каталог хранения. Относительный путь считается от каталога приложения.</summary>
    public string RootPath { get; set; } = "media";

    /// <summary>Внутренний префикс nginx для отдачи файлов через X-Accel-Redirect.</summary>
    public string InternalPrefix { get; set; } = "/media-internal";

    /// <summary>
    /// Минимальный возраст файла или каталога, после которого его можно удалять.
    /// Защищает от гонки с загрузкой, которая ещё не сохранила строку в БД.
    /// </summary>
    public TimeSpan CleanupGracePeriod { get; set; } = TimeSpan.FromMinutes(10);
}