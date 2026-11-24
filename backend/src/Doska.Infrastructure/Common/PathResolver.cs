namespace Doska.Infrastructure.Common;

/// <summary>
/// Приводит значение настройки пути к абсолютному: абсолютный путь используется как есть,
/// относительный — отсчитывается от корня содержимого приложения.
/// </summary>
public static class PathResolver
{
    /// <summary>Возвращает абсолютный путь для значения настройки.</summary>
    public static string Resolve(string configured, string contentRoot)
        => Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(contentRoot, configured);
}
