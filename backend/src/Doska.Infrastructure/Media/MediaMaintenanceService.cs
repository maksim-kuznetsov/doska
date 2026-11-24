using Doska.Application.Abstractions;
using Doska.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Doska.Infrastructure.Media;

/// <summary>
/// Идемпотентная очистка локального хранилища: удаляет каталоги объявлений, которых
/// нет в базе, и файлы, не привязанные к записям <c>ad_photos</c>.
/// </summary>
public sealed class MediaMaintenanceService(
    IAppDbContext db,
    IHostEnvironment environment,
    IOptions<MediaOptions> options) : IMediaMaintenance
{
    /// <inheritdoc />
    public async Task<MediaCleanupResult> CleanupAsync(CancellationToken cancellationToken = default)
    {
        var photosRoot = Path.Combine(ResolveRoot(), "photos");

        if (!Directory.Exists(photosRoot))
        {
            return new MediaCleanupResult(0, 0);
        }

        var adIds = await db.Ads
            .Select(ad => ad.Id)
            .ToListAsync(cancellationToken);
        var existingAds = adIds.ToHashSet();

        var photographs = await db.AdPhotos
            .Select(photo => new { photo.AdId, photo.FileName, photo.ThumbnailFileName })
            .ToListAsync(cancellationToken);

        var referencedFiles = photographs
            .GroupBy(photo => photo.AdId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .SelectMany(photo => new[] { photo.FileName, photo.ThumbnailFileName })
                    .ToHashSet(StringComparer.Ordinal));

        var removedDirectories = 0;
        var removedFiles = 0;

        foreach (var directory in Directory.EnumerateDirectories(photosRoot))
        {
            try
            {
                var directoryName = Path.GetFileName(directory);

                if (!Guid.TryParse(directoryName, out var adId) || !existingAds.Contains(adId))
                {
                    Directory.Delete(directory, recursive: true);
                    removedDirectories++;
                    continue;
                }

                var validFiles = referencedFiles.TryGetValue(adId, out var files)
                    ? files
                    : new HashSet<string>(StringComparer.Ordinal);

                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    if (!validFiles.Contains(Path.GetFileName(file)))
                    {
                        File.Delete(file);
                        removedFiles++;
                    }
                }

                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                    removedDirectories++;
                }
            }
            catch (IOException)
            {
                // Каталог занят другим процессом — оставляем до следующего запуска.
            }
            catch (UnauthorizedAccessException)
            {
                // Нет прав на удаление — оставляем до следующего запуска.
            }
        }

        return new MediaCleanupResult(removedDirectories, removedFiles);
    }

    private string ResolveRoot()
        => PathResolver.Resolve(options.Value.RootPath, environment.ContentRootPath);
}