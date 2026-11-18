using Doska.Application.Ads;

namespace Doska.Api.Contracts;

/// <summary>Запрос подачи объявления.</summary>
public sealed record CreateAdRequest(
    string Title,
    string Description,
    decimal Price,
    Guid CityId,
    Guid CategoryId,
    string Contacts);

/// <summary>Ответ по объявлению.</summary>
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
    DateTimeOffset? PublishedAt)
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
            ad.PublishedAt);
}
