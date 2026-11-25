using Doska.Api.Infrastructure;
using Doska.Application.Moderation;

namespace Doska.Api.Contracts;

/// <summary>Запрос решения с причиной (отклонение, скрытие).</summary>
public sealed record ReasonRequest(string Reason);

/// <summary>Элемент очереди проверки с данными объявления и контекстом автора.</summary>
public sealed record ModerationQueueItemResponse(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string CityName,
    string CategoryName,
    string Contacts,
    string AuthorEmail,
    bool AuthorIsTrusted,
    int AuthorApprovedAdsCount,
    DateTimeOffset SubmittedAt,
    IReadOnlyList<AdPhotoResponse> Photos)
{
    /// <summary>Создаёт ответ из элемента очереди.</summary>
    public static ModerationQueueItemResponse From(ModerationQueueItem item)
        => new(
            item.Id,
            item.Title,
            item.Description,
            item.Price,
            item.CityName,
            item.CategoryName,
            item.Contacts,
            item.AuthorEmail,
            item.AuthorIsTrusted,
            item.AuthorApprovedAdsCount,
            item.SubmittedAt,
            item.Photos
                .Select(photo => new AdPhotoResponse(
                    photo.Id,
                    MediaUrls.Photo(item.Id, photo.FileName),
                    MediaUrls.Photo(item.Id, photo.ThumbnailFileName),
                    photo.ContentType,
                    photo.ByteSize,
                    photo.Width,
                    photo.Height,
                    photo.SortOrder))
                .ToList());
}

/// <summary>Ответ о решении модератора по объявлению.</summary>
public sealed record ModerationDecisionResponse(Guid AdId, string Status, bool TrustGranted)
{
    /// <summary>Создаёт ответ из результата решения.</summary>
    public static ModerationDecisionResponse From(ModerationDecision decision)
        => new(decision.AdId, decision.Status, decision.TrustGranted);
}

/// <summary>Запрос жалобы на объявление.</summary>
public sealed record FileComplaintRequest(Guid AdId, string Reason);

/// <summary>Элемент очереди жалоб с данными объявления и сведениями о жалобе.</summary>
public sealed record ComplaintQueueItemResponse(
    Guid Id,
    Guid AdId,
    string AdTitle,
    string AdDescription,
    decimal AdPrice,
    string AdCityName,
    string AdCategoryName,
    string AdContacts,
    string AdStatus,
    string AdAuthorEmail,
    IReadOnlyList<AdPhotoResponse> AdPhotos,
    string AuthorEmail,
    string Reason,
    int OpenComplaintsCount,
    DateTimeOffset CreatedAt)
{
    /// <summary>Создаёт ответ из элемента очереди жалоб.</summary>
    public static ComplaintQueueItemResponse From(ComplaintQueueItem item)
        => new(
            item.Id,
            item.AdId,
            item.AdTitle,
            item.AdDescription,
            item.AdPrice,
            item.AdCityName,
            item.AdCategoryName,
            item.AdContacts,
            item.AdStatus.ToString(),
            item.AdAuthorEmail,
            item.AdPhotos
                .Select(photo => new AdPhotoResponse(
                    photo.Id,
                    MediaUrls.Photo(item.AdId, photo.FileName),
                    MediaUrls.Photo(item.AdId, photo.ThumbnailFileName),
                    photo.ContentType,
                    photo.ByteSize,
                    photo.Width,
                    photo.Height,
                    photo.SortOrder))
                .ToList(),
            item.AuthorEmail,
            item.Reason,
            item.OpenComplaintsCount,
            item.CreatedAt);
}

/// <summary>Запрос решения по жалобе.</summary>
public sealed record ResolveComplaintRequest(bool HideAd, string? HideReason);
