using Doska.Domain.Entities;
using Doska.Domain.Enums;

namespace Doska.Tests;

/// <summary>
/// Фабрика доменных сущностей для тестов. Заполняет обязательные данные и отметки времени,
/// чтобы тесты создавали сущности только через конструкторы и доменные методы.
/// </summary>
internal static class TestData
{
    /// <summary>Источник времени для сидов и доменных методов в тестах.</summary>
    public static TimeProvider Clock => TimeProvider.System;

    /// <summary>Создаёт учётную запись; при необходимости подтверждает почту и выдаёт доверие.</summary>
    public static User CreateUser(
        string? email = null,
        UserRole role = UserRole.User,
        bool confirmed = true,
        bool trusted = false,
        string passwordHash = "test-hash",
        DateTimeOffset? now = null)
    {
        var moment = now ?? DateTimeOffset.UtcNow;
        var user = new User(email ?? $"user-{Guid.NewGuid():N}@example.com", passwordHash, role, moment);

        if (confirmed)
        {
            user.ConfirmEmail(moment);
        }

        if (trusted)
        {
            user.GrantTrust(moment);
        }

        return user;
    }

    /// <summary>Создаёт город.</summary>
    public static City CreateCity(string? name = null, DateTimeOffset? now = null)
        => new(name ?? $"Город {Guid.NewGuid():N}", now ?? DateTimeOffset.UtcNow);

    /// <summary>Создаёт раздел верхнего уровня.</summary>
    public static Category CreateSection(string? name = null, int sortOrder = 1, DateTimeOffset? now = null)
        => new(name ?? $"Раздел {Guid.NewGuid():N}", sortOrder, now ?? DateTimeOffset.UtcNow);

    /// <summary>Создаёт подраздел в разделе верхнего уровня.</summary>
    public static Category CreateSubsection(
        Category section,
        string? name = null,
        int sortOrder = 0,
        DateTimeOffset? now = null)
        => new(name ?? $"Подраздел {Guid.NewGuid():N}", sortOrder, section, now ?? DateTimeOffset.UtcNow);

    /// <summary>Создаёт объявление в заданном статусе через конструктор и доменные методы.</summary>
    public static Ad CreateAd(
        Guid authorId,
        Guid categoryId,
        Guid cityId,
        string title = "Заголовок",
        string description = "Описание",
        decimal price = 1000m,
        string contacts = "+7 900 000-00-00",
        AdStatus status = AdStatus.Active,
        DateTimeOffset? publishedAt = null,
        DateTimeOffset? now = null)
    {
        var published = publishedAt ?? now ?? DateTimeOffset.UtcNow;
        var active = status is AdStatus.Active or AdStatus.Sold or AdStatus.Archived or AdStatus.Hidden;
        var ad = new Ad(authorId, categoryId, cityId, title, description, price, contacts, active, published);

        switch (status)
        {
            case AdStatus.Rejected:
                ad.Reject("Причина отклонения", published);
                break;
            case AdStatus.Hidden:
                ad.Hide("Причина скрытия", published);
                break;
            case AdStatus.Sold:
                ad.MarkSold(published);
                break;
            case AdStatus.Archived:
                ad.Archive(published);
                break;
        }

        return ad;
    }

    /// <summary>Создаёт фотографию объявления.</summary>
    public static AdPhoto CreatePhoto(
        Guid adId,
        string fileName = "photo.png",
        string? thumbnailFileName = null,
        int sortOrder = 1,
        DateTimeOffset? now = null)
        => new(
            adId,
            fileName,
            thumbnailFileName ?? $"thumb-{fileName}",
            "image/png",
            4,
            sortOrder,
            1,
            1,
            now ?? DateTimeOffset.UtcNow);

    /// <summary>Создаёт одноразовый токен.</summary>
    public static UserToken CreateToken(
        Guid userId,
        TokenPurpose purpose = TokenPurpose.EmailConfirmation,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? now = null)
    {
        var moment = now ?? DateTimeOffset.UtcNow;
        return new UserToken(
            userId,
            $"hash-{Guid.NewGuid():N}",
            purpose,
            expiresAt ?? moment.AddDays(1),
            moment);
    }
}
