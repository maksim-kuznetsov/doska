using Doska.Application.Abstractions;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Ads;

/// <summary>Порядок сортировки публичной выдачи.</summary>
public enum AdSort
{
    /// <summary>Сначала новые.</summary>
    Newest = 0,

    /// <summary>По возрастанию цены.</summary>
    PriceAscending = 1,

    /// <summary>По убыванию цены.</summary>
    PriceDescending = 2,
}

/// <summary>Условия публичной выдачи объявлений.</summary>
public sealed record AdSearchQuery(
    string? Text,
    Guid? CategoryId,
    Guid? CityId,
    decimal? MinPrice,
    decimal? MaxPrice,
    AdSort Sort,
    int Page,
    int PageSize);

/// <summary>Карточка объявления в публичной выдаче.</summary>
public sealed record AdCard(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string CityName,
    string CategoryName,
    string SectionName,
    DateTimeOffset PublishedAt);

/// <summary>Публичная лента и поиск по активным объявлениям.</summary>
public sealed class AdSearchService(IAppDbContext db)
{
    /// <summary>Количество объявлений на странице по умолчанию.</summary>
    public const int DefaultPageSize = 12;

    /// <summary>Максимальный размер страницы.</summary>
    public const int MaxPageSize = 50;

    /// <summary>Ищет активные объявления по тексту, фильтрам и страницам.</summary>
    public async Task<PagedResult<AdCard>> SearchAsync(
        AdSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.MinPrice is decimal minPrice
            && query.MaxPrice is decimal maxPrice
            && minPrice > maxPrice)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Минимальная цена не может быть больше максимальной.");
        }

        var page = query.Page > 0 ? query.Page : 1;
        var pageSize = query.PageSize > 0
            ? Math.Min(query.PageSize, MaxPageSize)
            : DefaultPageSize;

        var ads = db.Ads.Where(ad =>
            ad.Status == AdStatus.Active
            && ad.Author!.BlockedAt == null);

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            var text = query.Text.Trim().ToLowerInvariant();
            ads = ads.Where(ad =>
                ad.Title.ToLower().Contains(text)
                || ad.Description.ToLower().Contains(text));
        }

        if (query.CategoryId is Guid categoryId)
        {
            // Раздел верхнего уровня охватывает только активные подразделы. Прямой выбор
            // подраздела оставляет доступными его объявления даже после деактивации.
            ads = ads.Where(ad =>
                ad.CategoryId == categoryId
                || (ad.Category!.ParentId == categoryId && ad.Category.IsActive));
        }

        if (query.CityId is Guid cityId)
        {
            ads = ads.Where(ad => ad.CityId == cityId);
        }

        if (query.MinPrice is decimal min)
        {
            ads = ads.Where(ad => ad.Price >= min);
        }

        if (query.MaxPrice is decimal max)
        {
            ads = ads.Where(ad => ad.Price <= max);
        }

        var totalCount = await ads.CountAsync(cancellationToken);

        var ordered = query.Sort switch
        {
            AdSort.PriceAscending => ads
                .OrderBy(ad => ad.Price)
                .ThenByDescending(ad => ad.PublishedAt),
            AdSort.PriceDescending => ads
                .OrderByDescending(ad => ad.Price)
                .ThenByDescending(ad => ad.PublishedAt),
            _ => ads
                .OrderByDescending(ad => ad.PublishedAt)
                .ThenByDescending(ad => ad.CreatedAt),
        };

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ad => new AdCard(
                ad.Id,
                ad.Title,
                ad.Description,
                ad.Price,
                ad.City!.Name,
                ad.Category!.Name,
                ad.Category.Parent != null ? ad.Category.Parent.Name : ad.Category.Name,
                ad.PublishedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdCard>(items, totalCount, page, pageSize);
    }
}