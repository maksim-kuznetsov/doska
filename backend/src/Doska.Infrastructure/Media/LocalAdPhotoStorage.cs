using Doska.Application.Abstractions;
using Doska.Application.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Doska.Infrastructure.Media;

/// <summary>
/// Хранит фотографии на локальном томе и формирует уменьшенные копии.
/// Путь к каталогу задаётся настройкой <see cref="MediaOptions.RootPath"/>.
/// </summary>
public sealed class LocalAdPhotoStorage(
    IHostEnvironment environment,
    IOptions<MediaOptions> options) : IAdPhotoStorage
{
    private const int ThumbnailSize = 400;
    private const int ThumbnailQuality = 80;

    /// <inheritdoc />
    public async Task<StoredPhoto> SaveAsync(
        Guid adId,
        PhotoUpload upload,
        CancellationToken cancellationToken = default)
    {
        var extension = ExtensionFor(upload.ContentType);
        var directory = Path.Combine(RootPath, "photos", adId.ToString());
        Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var thumbnailFileName = $"{Guid.NewGuid():N}_thumb.jpg";
        var originalPath = Path.Combine(directory, fileName);
        var thumbnailPath = Path.Combine(directory, thumbnailFileName);

        await using var buffer = new MemoryStream();
        await upload.Content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        SKBitmap bitmap;

        try
        {
            bitmap = SKBitmap.Decode(bytes)
                ?? throw new InvalidOperationException("Формат изображения не распознан.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Не удалось прочитать изображение. Допустимы JPEG, PNG и WebP.");
        }

        using (bitmap)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;

            try
            {
                await File.WriteAllBytesAsync(originalPath, bytes, cancellationToken);

                var scale = Math.Min(
                    1d,
                    Math.Min((double)ThumbnailSize / width, (double)ThumbnailSize / height));
                var targetWidth = Math.Max(1, (int)Math.Round(width * scale));
                var targetHeight = Math.Max(1, (int)Math.Round(height * scale));

                using var resized = bitmap.Resize(
                    new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul),
                    SKFilterQuality.High)
                    ?? throw new InvalidOperationException("Не удалось уменьшить изображение.");

                using var image = SKImage.FromBitmap(resized);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, ThumbnailQuality)
                    ?? throw new InvalidOperationException("Не удалось закодировать уменьшенную копию.");

                await File.WriteAllBytesAsync(thumbnailPath, data.ToArray(), cancellationToken);

                return new StoredPhoto(
                    fileName,
                    thumbnailFileName,
                    upload.ContentType,
                    bytes.Length,
                    width,
                    height);
            }
            catch
            {
                TryDelete(originalPath);
                TryDelete(thumbnailPath);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(
        Guid adId,
        string fileName,
        string thumbnailFileName,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(RootPath, "photos", adId.ToString());

        TryDelete(Path.Combine(directory, fileName));
        TryDelete(Path.Combine(directory, thumbnailFileName));

        try
        {
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }
        catch (IOException)
        {
            // Каталог занят — файлы уже удалены, повторная попытка не требуется.
        }

        return Task.CompletedTask;
    }

    private string RootPath
    {
        get
        {
            var configured = options.Value.RootPath;

            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(environment.ContentRootPath, configured);
        }
    }

    private static string ExtensionFor(string contentType)
        => contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg",
        };

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Файл уже удалён или занят: удаление не является критичным.
        }
    }
}