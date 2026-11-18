using Doska.Application.Abstractions;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Ads;

/// <summary>Данные для подачи или редактирования объявления.</summary>
public sealed record CreateAdCommand(
    string Title,
    string Description,
    decimal Price,
    Guid CityId,
    Guid CategoryId,
    string Contacts);

/// <summary>Фотография объявления в представлении.</summary>
public sealed record AdPhotoSummary(
    Guid Id,
    string FileName,
    string ThumbnailFileName,
    string ContentType,
    long ByteSize,
    int Width,
    int Height,
    int SortOrder);

/// <summary>Краткое представление объявления для владельца.</summary>
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
    DateTimeOffset? PublishedAt,
    IReadOnlyList<AdPhotoSummary> Photos);

/// <summary>Подача, редактирование и ведение объявлений автором.</summary>
public sealed class AdService(IAppDbContext db, IAdPhotoStorage photoStorage, TimeProvider clock)
{
    /// <summary>Суточный лимит подачи для автора без доверия.</summary>
    public const int DailyLimitForUntrusted = 5;

    /// <summary>Минимальное число фотографий.</summary>
    public const int MinPhotos = 1;

    /// <summary>Максимальное число фотографий.</summary>
    public const int MaxPhotos = 5;

    /// <summary>Максимальный размер фотографии в байтах.</summary>
    public const long MaxPhotoBytes = 5 * 1024 * 1024;

    private const int MaxTitleLength = 200;
    private const int MaxContactsLength = 500;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    /// <summary>
    /// Создаёт объявление с фотографиями. Статус зависит от доверия автора:
    /// без доверия — «на проверке», с доверием — сразу «активно».
    /// </summary>
    public async Task<AdSummary> CreateAsync(
        Guid authorId,
        CreateAdCommand command,
        IReadOnlyList<PhotoUpload> photos,
        CancellationToken cancellationToken = default)
    {
        var author = await RequireAuthorAsync(authorId, cancellationToken);

        if (!author.IsEmailConfirmed)
        {
            throw new ServiceException(ServiceErrorKind.Forbidden, "Подтвердите email, чтобы подать объявление.");
        }

        ValidateCommand(command);
        ValidatePhotos(photos);

        var (city, category) = await ResolveCatalogAsync(command, cancellationToken);
        var now = clock.GetUtcNow();

        await EnsureDailyLimitAsync(author, now, cancellationToken);

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

        for (var index = 0; index < photos.Count; index++)
        {
            var stored = await photoStorage.SaveAsync(ad.Id, photos[index], cancellationToken);

            ad.Photos.Add(new AdPhoto
            {
                AdId = ad.Id,
                FileName = stored.FileName,
                ThumbnailFileName = stored.ThumbnailFileName,
                ContentType = stored.ContentType,
                ByteSize = stored.ByteSize,
                SortOrder = index + 1,
                Width = stored.Width,
                Height = stored.Height,
            });
        }

        db.Ads.Add(ad);
        await db.SaveChangesAsync(cancellationToken);

        return ToSummary(ad);
    }

    /// <summary>Возвращает собственные объявления автора со статусами и причинами.</summary>
    public async Task<IReadOnlyList<AdSummary>> GetMineAsync(
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        var ads = await db.Ads
            .Include(ad => ad.Photos)
            .Where(ad => ad.AuthorId == authorId)
            .OrderByDescending(ad => ad.CreatedAt)
            .ToListAsync(cancellationToken);

        return ads.Select(ToSummary).ToList();
    }

    /// <summary>Возвращает объявление автору или модератору.</summary>
    public async Task<AdSummary> GetByIdAsync(
        Guid adId,
        Guid viewerId,
        bool canModerate,
        CancellationToken cancellationToken = default)
    {
        var ad = await db.Ads
            .Include(candidate => candidate.Photos)
            .FirstOrDefaultAsync(candidate => candidate.Id == adId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.NotFound, "Объявление не найдено.");

        if (ad.AuthorId != viewerId && !canModerate)
        {
            throw new ServiceException(ServiceErrorKind.Forbidden, "Объявление принадлежит другому автору.");
        }

        return ToSummary(ad);
    }

    /// <summary>Изменяет объявление автора; непроверенный автор возвращает его на проверку.</summary>
    public async Task<AdSummary> UpdateAsync(
        Guid adId,
        Guid authorId,
        CreateAdCommand command,
        CancellationToken cancellationToken = default)
    {
        var (ad, author) = await LoadOwnedAsync(adId, authorId, cancellationToken);

        if (author.IsBlocked)
        {
            throw new ServiceException(ServiceErrorKind.Forbidden, "Аккаунт заблокирован.");
        }

        ValidateCommand(command);
        var (city, category) = await ResolveCatalogAsync(command, cancellationToken);

        ad.Title = command.Title.Trim();
        ad.Description = command.Description.Trim();
        ad.Price = command.Price;
        ad.Contacts = command.Contacts.Trim();
        ad.CityId = city.Id;
        ad.CategoryId = category.Id;
        ad.CompleteEdit(author.IsTrusted);

        await db.SaveChangesAsync(cancellationToken);
        return ToSummary(ad);
    }

    /// <summary>Удаляет объявление автора вместе с файлами фотографий.</summary>
    public async Task DeleteAsync(
        Guid adId,
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        var (ad, _) = await LoadOwnedAsync(adId, authorId, cancellationToken);

        foreach (var photo in ad.Photos)
        {
            await photoStorage.DeleteAsync(
                ad.Id,
                photo.FileName,
                photo.ThumbnailFileName,
                cancellationToken);
        }

        db.Ads.Remove(ad);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Отмечает активное объявление автора проданным.</summary>
    public async Task<AdSummary> MarkSoldAsync(
        Guid adId,
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        var (ad, _) = await LoadOwnedAsync(adId, authorId, cancellationToken);

        ad.MarkSold();
        await db.SaveChangesAsync(cancellationToken);
        return ToSummary(ad);
    }

    /// <summary>Возвращает проданное объявление в продажу по уровню доверия автора.</summary>
    public async Task<AdSummary> ReturnToSaleAsync(
        Guid adId,
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        var (ad, author) = await LoadOwnedAsync(adId, authorId, cancellationToken);

        ad.ReturnToSale(author.IsTrusted, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return ToSummary(ad);
    }

    /// <summary>Продлевает активное или архивное объявление на новый срок.</summary>
    public async Task<AdSummary> RenewAsync(
        Guid adId,
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        var (ad, _) = await LoadOwnedAsync(adId, authorId, cancellationToken);

        ad.Renew(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return ToSummary(ad);
    }

    /// <summary>
    /// Архивирует активные объявления старше срока публикации. Повторный запуск
    /// не меняет уже архивные объявления.
    /// </summary>
    public async Task<int> ArchiveExpiredAsync(CancellationToken cancellationToken = default)
    {
        var threshold = clock.GetUtcNow().AddDays(-Ad.LifetimeDays);

        var expired = await db.Ads
            .Where(ad => ad.Status == AdStatus.Active && ad.PublishedAt <= threshold)
            .ToListAsync(cancellationToken);

        foreach (var ad in expired)
        {
            ad.Archive();
        }

        if (expired.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return expired.Count;
    }

    private async Task<User> RequireAuthorAsync(Guid authorId, CancellationToken cancellationToken)
    {
        var author = await db.Users.FirstOrDefaultAsync(user => user.Id == authorId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.Unauthorized, "Требуется вход.");

        if (author.IsBlocked)
        {
            throw new ServiceException(ServiceErrorKind.Forbidden, "Аккаунт заблокирован.");
        }

        return author;
    }

    private async Task<(Ad Ad, User Author)> LoadOwnedAsync(
        Guid adId,
        Guid authorId,
        CancellationToken cancellationToken)
    {
        var ad = await db.Ads
            .Include(candidate => candidate.Photos)
            .Include(candidate => candidate.Author)
            .FirstOrDefaultAsync(candidate => candidate.Id == adId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.NotFound, "Объявление не найдено.");

        if (ad.AuthorId != authorId)
        {
            throw new ServiceException(
                ServiceErrorKind.Forbidden,
                "Можно изменять только свои объявления.");
        }

        return (ad, ad.Author!);
    }

    private async Task EnsureDailyLimitAsync(User author, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (author.IsTrusted)
        {
            return;
        }

        var since = now.AddDays(-1);
        var submittedToday = await db.Ads.CountAsync(
            ad => ad.AuthorId == author.Id && ad.CreatedAt >= since,
            cancellationToken);

        if (submittedToday >= DailyLimitForUntrusted)
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Суточный лимит подачи исчерпан. Попробуйте снова позже.");
        }
    }

    private async Task<(City City, Category Category)> ResolveCatalogAsync(
        CreateAdCommand command,
        CancellationToken cancellationToken)
    {
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

        return (city, category);
    }

    private static void ValidateCommand(CreateAdCommand command)
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

    private static void ValidatePhotos(IReadOnlyList<PhotoUpload> photos)
    {
        if (photos.Count is < MinPhotos or > MaxPhotos)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                $"Приложите от {MinPhotos} до {MaxPhotos} фотографий.");
        }

        foreach (var photo in photos)
        {
            if (!AllowedContentTypes.Contains(photo.ContentType))
            {
                throw new ServiceException(
                    ServiceErrorKind.Validation,
                    "Допустимы фотографии JPEG, PNG и WebP.");
            }

            if (photo.Length <= 0 || photo.Length > MaxPhotoBytes)
            {
                throw new ServiceException(
                    ServiceErrorKind.Validation,
                    "Размер фотографии не должен превышать 5 МБ.");
            }
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
            ad.PublishedAt,
            ad.Photos
                .OrderBy(photo => photo.SortOrder)
                .Select(photo => new AdPhotoSummary(
                    photo.Id,
                    photo.FileName,
                    photo.ThumbnailFileName,
                    photo.ContentType,
                    photo.ByteSize,
                    photo.Width,
                    photo.Height,
                    photo.SortOrder))
                .ToList());
}