using Doska.Application.Abstractions;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Ads;

/// <summary>Данные для подачи объявления.</summary>
public sealed record CreateAdCommand(
    string Title,
    string Description,
    decimal Price,
    Guid CityId,
    Guid CategoryId,
    string Contacts);

/// <summary>Краткое представление объявления для владельца и лент.</summary>
public sealed record AdSummary(
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
    DateTimeOffset? PublishedAt);

/// <summary>Подача объявлений и список собственных объявлений автора.</summary>
public sealed class AdService(IAppDbContext db, TimeProvider clock)
{
    /// <summary>Суточный лимит подачи для автора без доверия.</summary>
    public const int DailyLimitForUntrusted = 5;

    private const int MaxTitleLength = 200;
    private const int MaxContactsLength = 500;

    /// <summary>
    /// Создаёт объявление. Статус зависит от доверия автора: без доверия — «на проверке»
    /// с суточным лимитом, с доверием — сразу «активно».
    /// </summary>
    public async Task<AdSummary> CreateAsync(
        Guid authorId,
        CreateAdCommand command,
        CancellationToken cancellationToken = default)
    {
        var author = await db.Users.FirstOrDefaultAsync(user => user.Id == authorId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.Unauthorized, "Требуется вход.");

        if (author.IsBlocked)
        {
            throw new ServiceException(ServiceErrorKind.Forbidden, "Аккаунт заблокирован.");
        }

        if (!author.IsEmailConfirmed)
        {
            throw new ServiceException(ServiceErrorKind.Forbidden, "Подтвердите email, чтобы подать объявление.");
        }

        Validate(command);

        var city = await db.Cities.FirstOrDefaultAsync(candidate => candidate.Id == command.CityId, cancellationToken);
        if (city is null || !city.IsActive)
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Город не найден или недоступен.");
        }

        var category = await db.Categories.FirstOrDefaultAsync(
            candidate => candidate.Id == command.CategoryId,
            cancellationToken);
        if (category is null || !category.IsActive || category.IsSection)
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Подраздел категории не найден или недоступен.");
        }

        var now = clock.GetUtcNow();

        if (!author.IsTrusted)
        {
            var since = now.AddDays(-1);
            var submittedToday = await db.Ads.CountAsync(
                ad => ad.AuthorId == authorId && ad.CreatedAt >= since,
                cancellationToken);

            if (submittedToday >= DailyLimitForUntrusted)
            {
                throw new ServiceException(
                    ServiceErrorKind.Conflict,
                    "Суточный лимит подачи исчерпан. Попробуйте снова позже.");
            }
        }

        var ad = new Ad
        {
            AuthorId = authorId,
            CategoryId = category.Id,
            CityId = city.Id,
            Title = command.Title.Trim(),
            Description = command.Description.Trim(),
            Price = command.Price,
            Contacts = command.Contacts.Trim(),
            Status = author.IsTrusted ? AdStatus.Active : AdStatus.OnModeration,
            PublishedAt = author.IsTrusted ? now : default,
        };

        db.Ads.Add(ad);
        await db.SaveChangesAsync(cancellationToken);

        return ToSummary(ad);
    }

    /// <summary>Возвращает собственные объявления автора со статусами и причинами.</summary>
    public async Task<IReadOnlyList<AdSummary>> GetMineAsync(
        Guid authorId,
        CancellationToken cancellationToken = default)
        => await db.Ads
            .Where(ad => ad.AuthorId == authorId)
            .OrderByDescending(ad => ad.CreatedAt)
            .Select(ad => new AdSummary(
                ad.Id,
                ad.Title,
                ad.Description,
                ad.Price,
                ad.CityId,
                ad.CategoryId,
                ad.Contacts,
                ad.Status.ToString(),
                ad.ModerationReason,
                ad.CreatedAt,
                ad.PublishedAt))
            .ToListAsync(cancellationToken);

    private static void Validate(CreateAdCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > MaxTitleLength)
        {
            throw new ServiceException(ServiceErrorKind.Validation, $"Заголовок обязателен и не длиннее {MaxTitleLength} символов.");
        }

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Описание обязательно.");
        }

        if (command.Price < 0)
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Цена должна быть неотрицательным числом.");
        }

        if (string.IsNullOrWhiteSpace(command.Contacts) || command.Contacts.Trim().Length > MaxContactsLength)
        {
            throw new ServiceException(ServiceErrorKind.Validation, $"Контакты обязательны и не длиннее {MaxContactsLength} символов.");
        }
    }

    private static AdSummary ToSummary(Ad ad)
        => new(
            ad.Id,
            ad.Title,
            ad.Description,
            ad.Price,
            ad.CityId,
            ad.CategoryId,
            ad.Contacts,
            ad.Status.ToString(),
            ad.ModerationReason,
            ad.CreatedAt,
            ad.PublishedAt);
}
