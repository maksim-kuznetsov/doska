using Doska.Api.Infrastructure;
using Doska.Application.Ads;

namespace Doska.Api.Contracts;

/// <summary>Multipart-запрос подачи объявления с фотографиями.</summary>
public sealed class CreateAdForm
{
    /// <summary>Заголовок.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Описание.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Цена в рублях.</summary>
    public decimal Price { get; set; }

    /// <summary>Идентификатор города.</summary>
    public Guid CityId { get; set; }

    /// <summary>Идентификатор подраздела.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Контакты.</summary>
    public string Contacts { get; set; } = string.Empty;

    /// <summary>Фотографии, от 1 до 5.</summary>
    public List<IFormFile> Photos { get; set; } = [];
}

/// <summary>Запрос редактирования объявления.</summary>
public sealed record UpdateAdRequest(
    string Title,
    string Description,
    decimal Price,
    Guid CityId,
    Guid CategoryId,
    string Contacts);

/// <summary>Фотография объявления.</summary>
public sealed record AdPhotoResponse(
    Guid Id,
    string Url,
    string ThumbnailUrl,
    string ContentType,
    long ByteSize,
    int Width,
    int Height,
    int SortOrder);

/// <summary>Ответ по объявлению автора.</summary>
public sealed record AdResponse(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    Guid CityId,
    Guid CategoryId,
    string Contacts,
    string Status,
    string? ModerationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<AdPhotoResponse> Photos)
{
    /// <summary>Создаёт ответ из представления объявления.</summary>
    public static AdResponse From(AdSummary ad)
        => new(
            ad.Id,
            ad.Title,
            ad.Description,
            ad.Price,
            ad.CityId,
            ad.CategoryId,
            ad.Contacts,
            ad.Status,
            ad.ModerationReason,
            ad.CreatedAt,
            ad.PublishedAt,
            ad.Photos
                .Select(photo => new AdPhotoResponse(
                    photo.Id,
                    MediaUrls.Photo(ad.Id, photo.FileName),
                    MediaUrls.Photo(ad.Id, photo.ThumbnailFileName),
                    photo.ContentType,
                    photo.ByteSize,
                    photo.Width,
                    photo.Height,
                    photo.SortOrder))
                .ToList());
}