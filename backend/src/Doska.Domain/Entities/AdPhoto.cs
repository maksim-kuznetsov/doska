namespace Doska.Domain.Entities;

/// <summary>
/// Фотография объявления. Размер и порядок ограничены: не более пяти фото
/// с порядковыми номерами от 1 до 5.
/// </summary>
public class AdPhoto : Entity
{
    /// <summary>Объявление, к которому относится фотография.</summary>
    public Guid AdId { get; set; }

    /// <summary>Объявление, к которому относится фотография.</summary>
    public Ad? Ad { get; set; }

    /// <summary>Имя файла изображения в хранилище.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Имя файла уменьшенной копии в хранилище.</summary>
    public string ThumbnailFileName { get; set; } = string.Empty;

    /// <summary>MIME-тип файла.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Размер файла в байтах.</summary>
    public long ByteSize { get; set; }

    /// <summary>Порядок вывода фотографии в объявлении: от 1 до 5.</summary>
    public int SortOrder { get; set; }

    /// <summary>Ширина изображения в пикселях.</summary>
    public int Width { get; set; }

    /// <summary>Высота изображения в пикселях.</summary>
    public int Height { get; set; }
}
