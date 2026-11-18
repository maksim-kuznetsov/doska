namespace Doska.Application.Abstractions;

/// <summary>Загружаемая фотография объявления.</summary>
public sealed record PhotoUpload(Stream Content, string FileName, string ContentType, long Length);

/// <summary>Сохранённая фотография: оригинал и уменьшенная копия.</summary>
public sealed record StoredPhoto(
    string FileName,
    string ThumbnailFileName,
    string ContentType,
    long ByteSize,
    int Width,
    int Height);

/// <summary>
/// Хранилище фотографий объявлений. Реализация по умолчанию — локальный том;
/// контракт позволяет заменить её на S3-совместимое хранилище.
/// </summary>
public interface IAdPhotoStorage
{
    /// <summary>Сохраняет оригинал и формирует уменьшенную копию.</summary>
    Task<StoredPhoto> SaveAsync(Guid adId, PhotoUpload upload, CancellationToken cancellationToken = default);

    /// <summary>Удаляет файлы фотографии.</summary>
    Task DeleteAsync(
        Guid adId,
        string fileName,
        string thumbnailFileName,
        CancellationToken cancellationToken = default);
}