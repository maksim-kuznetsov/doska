namespace Doska.Application.Abstractions;

/// <summary>Результат очистки хранилища медиафайлов.</summary>
public sealed record MediaCleanupResult(int RemovedDirectories, int RemovedFiles);

/// <summary>Обслуживание хранилища медиафайлов.</summary>
public interface IMediaMaintenance
{
    /// <summary>
    /// Удаляет каталоги объявлений, которых нет в базе, и файлы, не привязанные
    /// к записям <c>ad_photos</c>. Повторный запуск ничего не меняет.
    /// </summary>
    Task<MediaCleanupResult> CleanupAsync(CancellationToken cancellationToken = default);
}