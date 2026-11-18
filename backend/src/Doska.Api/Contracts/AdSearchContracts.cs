using Doska.Api.Infrastructure;
using Doska.Application.Ads;

namespace Doska.Api.Contracts;

/// <summary>Параметры публичной ленты и поиска.</summary>
public sealed class AdSearchRequest
{
    /// <summary>Поисковая строка по заголовку и описанию.</summary>
    public string? Query { get; set; }

    /// <summary>Раздел верхнего уровня или подраздел.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Город.</summary>
    public Guid? CityId { get; set; }

    /// <summary>Минимальная цена включительно.</summary>
    public decimal? MinPrice { get; set; }

    /// <summary>Максимальная цена включительно.</summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>Сортировка: <c>newest</c>, <c>price_asc</c> или <c>price_desc</c>.</summary>
    public string? Sort { get; set; }

    /// <summary>Номер страницы, начиная с 1.</summary>
    public int? Page { get; set; }

    /// <summary>Размер страницы.</summary>
    public int? PageSize { get; set; }
}

/// <summary>Карточка объявления в публичной выдаче.</summary>
public sealed record AdCardResponse(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string CityName,
    string CategoryName,
    string SectionName,
    DateTimeOffset PublishedAt,
    string? ThumbnailUrl)
{
    /// <summary>Создаёт ответ из карточки.</summary>
    public static AdCardResponse From(AdCard card)
        => new(
            card.Id,
            card.Title,
            card.Description,
            card.Price,
            card.CityName,
            card.CategoryName,
            card.SectionName,
            card.PublishedAt,
            card.ThumbnailFileName is null
                ? null
                : MediaUrls.Photo(card.Id, card.ThumbnailFileName));
}

/// <summary>Страница результатов.</summary>
public sealed record PagedResultResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);