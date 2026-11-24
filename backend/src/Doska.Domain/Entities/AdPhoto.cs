namespace Doska.Domain.Entities;

/// <summary>
/// Фотография объявления. Размер и порядок ограничены: не более пяти фото
/// с порядковыми номерами от 1 до 5.
/// </summary>
public class AdPhoto : Entity
{
    /// <summary>Объявление, к которому относится фотография.</summary>
    public Guid AdId { get; private set; }

    /// <summary>Объявление, к которому относится фотография.</summary>
    public Ad? Ad { get; private set; }

    /// <summary>Имя файла изображения в хранилище.</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>Имя файла уменьшенной копии в хранилище.</summary>
    public string ThumbnailFileName { get; private set; } = string.Empty;

    /// <summary>MIME-тип файла.</summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>Размер файла в байтах.</summary>
    public long ByteSize { get; private set; }

    /// <summary>Порядок вывода фотографии в объявлении: от 1 до 5.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Ширина изображения в пикселях.</summary>
    public int Width { get; private set; }

    /// <summary>Высота изображения в пикселях.</summary>
    public int Height { get; private set; }

    /// <summary>Создаёт пустую фотографию: используется EF Core при чтении из базы.</summary>
    private AdPhoto()
    {
    }

    /// <summary>Создаёт фотографию объявления.</summary>
    /// <param name="adId">Объявление.</param>
    /// <param name="fileName">Имя файла изображения.</param>
    /// <param name="thumbnailFileName">Имя файла уменьшенной копии.</param>
    /// <param name="contentType">MIME-тип файла.</param>
    /// <param name="byteSize">Размер файла в байтах.</param>
    /// <param name="sortOrder">Порядок вывода.</param>
    /// <param name="width">Ширина изображения.</param>
    /// <param name="height">Высота изображения.</param>
    /// <param name="now">Момент создания.</param>
    public AdPhoto(
        Guid adId,
        string fileName,
        string thumbnailFileName,
        string contentType,
        long byteSize,
        int sortOrder,
        int width,
        int height,
        DateTimeOffset now)
        : base(now)
    {
        AdId = adId;
        FileName = fileName;
        ThumbnailFileName = thumbnailFileName;
        ContentType = contentType;
        ByteSize = byteSize;
        SortOrder = sortOrder;
        Width = width;
        Height = height;
    }
}
