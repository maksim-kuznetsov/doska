using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Doska.Application.Abstractions;
using Doska.Application.Ads;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace Doska.Tests;

/// <summary>Подача с фотографиями и жизненный цикл объявления.</summary>
public sealed class ClassifiedAdsApiTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string Password = "secret123";
    private const string SectionPrefix = "Объяв-раздел ";
    private const string SubsectionPrefix = "Объяв-подраздел ";
    private const string CityPrefix = "Объяв-город ";
    private const string AdPrefix = "Объяв-тест ";

    private static readonly byte[] TestImage = CreateTestImage();

    private readonly ApiFactory _factory;

    public ClassifiedAdsApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Тесты не требуют подготовки.</summary>
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    /// <summary>Удаляет данные, созданные тестом.</summary>
    public async ValueTask DisposeAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        if (!await TestDatabase.IsReachableAsync(_factory, ct))
        {
            return;
        }

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            db.Ads.RemoveRange(db.Ads.Where(ad => ad.Title.StartsWith(AdPrefix)));
            await db.SaveChangesAsync(ct);

            db.Categories.RemoveRange(
                db.Categories.Where(category => category.Name.StartsWith(SubsectionPrefix)));
            await db.SaveChangesAsync(ct);

            db.Categories.RemoveRange(
                db.Categories.Where(category => category.Name.StartsWith(SectionPrefix)));
            await db.SaveChangesAsync(ct);

            db.Cities.RemoveRange(db.Cities.Where(city => city.Name.StartsWith(CityPrefix)));
            await db.SaveChangesAsync(ct);

            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith("ads-")));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Create_requires_between_one_and_five_photos()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        await RegisterAuthorAsync(author, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();

        var withoutPhotos = await CreateAdAsync(author, $"{AdPrefix}Без фото", cityId, categoryId, 0, ct);
        Assert.Equal(HttpStatusCode.BadRequest, withoutPhotos.StatusCode);

        var tooMany = await CreateAdAsync(author, $"{AdPrefix}Шесть фото", cityId, categoryId, 6, ct);
        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);

        var accepted = await CreateAdAsync(author, $"{AdPrefix}Одно фото", cityId, categoryId, 1, ct);
        var ad = await ReadCreatedAsync(accepted, ct);
        Assert.Single(ad.Photos);
    }

    [Fact]
    public async Task Create_rejects_unsupported_and_oversized_photos()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        await RegisterAuthorAsync(author, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();

        using (var unsupported = BuildForm($"{AdPrefix}Текстовый файл", cityId, categoryId))
        {
            AddPhoto(unsupported, "не изображение"u8.ToArray(), "text/plain", "note.txt");
            var response = await author.PostContentAsync("/api/ads", unsupported, ct);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using (var oversized = BuildForm($"{AdPrefix}Большое фото", cityId, categoryId))
        {
            AddPhoto(oversized, new byte[AdService.MaxPhotoBytes + 1], "image/png");
            var response = await author.PostContentAsync("/api/ads", oversized, ct);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task Create_rejects_price_above_database_range()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        await RegisterAuthorAsync(author, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();

        using (var tooExpensive = BuildForm(
                   $"{AdPrefix}Слишком дорогое",
                   cityId,
                   categoryId,
                   AdService.MaxPrice + 1m))
        {
            AddPhoto(tooExpensive, TestImage, "image/png", "photo.png");
            var response = await author.PostContentAsync("/api/ads", tooExpensive, ct);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync(ct);
            Assert.Contains("9 999 999 999", body);
        }

        using var atLimit = BuildForm($"{AdPrefix}На пределе", cityId, categoryId, AdService.MaxPrice);
        AddPhoto(atLimit, TestImage, "image/png", "photo.png");
        var created = await author.PostContentAsync("/api/ads", atLimit, ct);
        var ad = await ReadCreatedAsync(created, ct);

        Assert.Equal(AdService.MaxPrice, ad.Price);
    }

    [Fact]
    public async Task Create_accepts_photo_near_three_megabytes()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        await RegisterAuthorAsync(author, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();

        var image = CreateLargeTestImage();
        Assert.InRange(image.Length, 1_000_000, (int)AdService.MaxPhotoBytes);

        using var form = BuildForm($"{AdPrefix}Фото 3,4 МБ", cityId, categoryId);
        AddPhoto(form, image, "image/png", "large.png");

        var ad = await ReadCreatedAsync(
            await author.PostContentAsync("/api/ads", form, ct),
            ct);

        var photo = Assert.Single(ad.Photos);
        Assert.Equal(image.Length, photo.ByteSize);
    }

    [Fact]
    public async Task Media_endpoint_serves_original_and_thumbnail()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        await RegisterAuthorAsync(author, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();

        var ad = await ReadCreatedAsync(
            await CreateAdAsync(author, $"{AdPrefix}С фотографией", cityId, categoryId, 1, ct),
            ct);

        var photo = Assert.Single(ad.Photos);

        using var anonymous = _factory.CreateClient();
        var original = await anonymous.GetAsync(photo.Url, ct);
        var thumbnail = await anonymous.GetAsync(photo.ThumbnailUrl, ct);

        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        Assert.Equal("image/png", original.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, thumbnail.StatusCode);
        Assert.Equal("image/jpeg", thumbnail.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Foreign_author_cannot_edit_or_delete_ad()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var owner = _factory.CreateClient();
        await RegisterAuthorAsync(owner, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();
        var ad = await ReadCreatedAsync(
            await CreateAdAsync(owner, $"{AdPrefix}Чужое объявление", cityId, categoryId, 1, ct),
            ct);

        using var stranger = _factory.CreateClient();
        await RegisterAuthorAsync(stranger, ct);

        var update = await stranger.PutJsonAsync(
            $"/api/ads/{ad.Id}",
            new
            {
                title = "Взлом",
                description = "Описание",
                price = 100m,
                cityId,
                categoryId,
                contacts = "+7 900 000-00-00",
            },
            ct);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);

        var delete = await stranger.DeleteWithCsrfAsync($"/api/ads/{ad.Id}", ct);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Untrusted_edit_returns_active_ad_to_moderation_and_trusted_keeps_status()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var untrusted = _factory.CreateClient();
        var untrustedUser = await RegisterAuthorAsync(untrusted, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();
        var activeAdId = await CreateDbAdAsync(
            untrustedUser.Id,
            categoryId,
            cityId,
            $"{AdPrefix}Активное без доверия",
            AdStatus.Active,
            DateTimeOffset.UtcNow);

        var untrustedUpdate = await untrusted.PutJsonAsync(
            $"/api/ads/{activeAdId}",
            new
            {
                title = $"{AdPrefix}Изменённое",
                description = "Новое описание",
                price = 200m,
                cityId,
                categoryId,
                contacts = "+7 900 000-00-00",
            },
            ct);

        Assert.Equal(HttpStatusCode.OK, untrustedUpdate.StatusCode);
        var untrustedAd = await untrustedUpdate.Content.ReadFromJsonAsync<AdPayload>(ct);
        Assert.Equal("OnModeration", untrustedAd!.Status);

        using var trusted = _factory.CreateClient();
        await RegisterAuthorAsync(trusted, ct, trustedAuthor: true);
        var trustedAd = await ReadCreatedAsync(
            await CreateAdAsync(trusted, $"{AdPrefix}Доверенное объявление", cityId, categoryId, 1, ct),
            ct);
        Assert.Equal("Active", trustedAd.Status);

        var trustedUpdate = await trusted.PutJsonAsync(
            $"/api/ads/{trustedAd.Id}",
            new
            {
                title = $"{AdPrefix}Изменённое доверенным",
                description = "Новое описание",
                price = 300m,
                cityId,
                categoryId,
                contacts = "+7 900 000-00-00",
            },
            ct);

        Assert.Equal(HttpStatusCode.OK, trustedUpdate.StatusCode);
        var updated = await trustedUpdate.Content.ReadFromJsonAsync<AdPayload>(ct);
        Assert.Equal("Active", updated!.Status);
    }

    [Fact]
    public async Task Sold_hides_from_feed_and_return_restores_status()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var trusted = _factory.CreateClient();
        await RegisterAuthorAsync(trusted, ct, trustedAuthor: true);
        var (cityId, categoryId) = await CreateCatalogAsync();
        var marker = $"SoldMarker{Guid.NewGuid():N}";
        var ad = await ReadCreatedAsync(
            await CreateAdAsync(trusted, $"{AdPrefix}{marker}", cityId, categoryId, 1, ct),
            ct);

        var sold = await trusted.PostJsonAsync($"/api/ads/{ad.Id}/sold", null, ct);
        Assert.Equal(HttpStatusCode.OK, sold.StatusCode);

        using var anonymous = _factory.CreateClient();
        var hidden = await anonymous.GetFromJsonAsync<AdPagePayload>($"/api/ads?query={marker}", ct);
        Assert.Empty(hidden!.Items);

        var returned = await trusted.PostJsonAsync($"/api/ads/{ad.Id}/return-to-sale", null, ct);
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        var returnedAd = await returned.Content.ReadFromJsonAsync<AdPayload>(ct);
        Assert.Equal("Active", returnedAd!.Status);

        var visible = await anonymous.GetFromJsonAsync<AdPagePayload>($"/api/ads?query={marker}", ct);
        Assert.Single(visible!.Items);
    }

    [Fact]
    public async Task Renew_republishes_archived_ad()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        var user = await RegisterAuthorAsync(author, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();
        var marker = $"RenewMarker{Guid.NewGuid():N}";
        var adId = await CreateDbAdAsync(
            user.Id,
            categoryId,
            cityId,
            $"{AdPrefix}{marker}",
            AdStatus.Archived,
            DateTimeOffset.UtcNow.AddDays(-10));

        var renewed = await author.PostJsonAsync($"/api/ads/{adId}/renew", null, ct);
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        var ad = await renewed.Content.ReadFromJsonAsync<AdPayload>(ct);
        Assert.Equal("Active", ad!.Status);

        using var anonymous = _factory.CreateClient();
        var feed = await anonymous.GetFromJsonAsync<AdPagePayload>($"/api/ads?query={marker}", ct);
        Assert.Single(feed!.Items);
    }

    [Fact]
    public async Task Archive_expired_archives_only_ads_older_than_seven_days()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        var user = await RegisterAuthorAsync(author, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();

        var expiredId = await CreateDbAdAsync(
            user.Id, categoryId, cityId, $"{AdPrefix}Старое", AdStatus.Active, DateTimeOffset.UtcNow.AddDays(-8));
        var freshId = await CreateDbAdAsync(
            user.Id, categoryId, cityId, $"{AdPrefix}Свежее", AdStatus.Active, DateTimeOffset.UtcNow.AddDays(-1));

        await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var service = provider.GetRequiredService<AdService>();
            await service.ArchiveExpiredAsync(ct);
            return 0;
        });

        var statuses = await TestDatabase.WithContextAsync(_factory, db => db.Ads
            .Where(ad => ad.Id == expiredId || ad.Id == freshId)
            .Select(ad => new { ad.Id, ad.Status })
            .ToListAsync(ct));

        Assert.Equal(AdStatus.Archived, statuses.Single(status => status.Id == expiredId).Status);
        Assert.Equal(AdStatus.Active, statuses.Single(status => status.Id == freshId).Status);
    }

    [Fact]
    public async Task Delete_removes_ad_and_its_media()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        await RegisterAuthorAsync(author, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();
        var ad = await ReadCreatedAsync(
            await CreateAdAsync(author, $"{AdPrefix}Удаляемое объявление", cityId, categoryId, 1, ct),
            ct);
        var photo = Assert.Single(ad.Photos);

        var deleted = await author.DeleteWithCsrfAsync($"/api/ads/{ad.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var mine = await author.GetFromJsonAsync<List<AdPayload>>("/api/ads/mine", ct);
        Assert.DoesNotContain(mine!, candidate => candidate.Id == ad.Id);

        using var anonymous = _factory.CreateClient();
        var media = await anonymous.GetAsync(photo.Url, ct);
        Assert.Equal(HttpStatusCode.NotFound, media.StatusCode);
    }

    [Fact]
    public async Task Public_view_shows_active_ad_and_hides_inactive()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        var authorUser = await RegisterAuthorAsync(author, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();

        var activeId = await CreateDbAdAsync(
            authorUser.Id,
            categoryId,
            cityId,
            $"{AdPrefix}Активное для просмотра",
            AdStatus.Active,
            DateTimeOffset.UtcNow);
        var moderationId = await CreateDbAdAsync(
            authorUser.Id,
            categoryId,
            cityId,
            $"{AdPrefix}На проверке для просмотра",
            AdStatus.OnModeration,
            default);

        using var anonymous = _factory.CreateClient();

        var active = await anonymous.GetAsync($"/api/ads/{activeId}", ct);
        Assert.Equal(HttpStatusCode.OK, active.StatusCode);

        var hidden = await anonymous.GetAsync($"/api/ads/{moderationId}", ct);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        var ownInactive = await author.GetAsync($"/api/ads/{moderationId}", ct);
        Assert.Equal(HttpStatusCode.OK, ownInactive.StatusCode);
    }

    [Fact]
    public async Task Moderator_sees_non_active_ad_by_id()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        var authorUser = await RegisterAuthorAsync(author, ct);
        var (cityId, categoryId) = await CreateCatalogAsync();
        var moderationId = await CreateDbAdAsync(
            authorUser.Id,
            categoryId,
            cityId,
            $"{AdPrefix}На проверке для модератора",
            AdStatus.OnModeration,
            default);

        using var moderator = _factory.CreateClient();
        await LoginModeratorAsync(moderator, ct);

        var response = await moderator.GetAsync($"/api/ads/{moderationId}", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private async Task LoginModeratorAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var email = $"ads-moderator-{Guid.NewGuid():N}@example.com";

        await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var passwordHasher = provider.GetRequiredService<IPasswordHasher>();

            db.Users.Add(TestData.CreateUser(
                email,
                UserRole.Moderator,
                passwordHash: passwordHasher.Hash(Password)));

            await db.SaveChangesAsync(cancellationToken);
            return 0;
        });

        var response = await client.PostJsonAsync(
            "/api/auth/login",
            new { email, password = Password },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<UserPayload> RegisterAuthorAsync(
        HttpClient client,
        CancellationToken cancellationToken,
        bool trustedAuthor = false)
    {
        var email = $"ads-{Guid.NewGuid():N}@example.com";
        var response = await client.PostJsonAsync(
            "/api/auth/register",
            new { email, password = Password },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<UserPayload>(cancellationToken))!;

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var stored = await db.Users.FirstAsync(candidate => candidate.Id == user.Id, cancellationToken);
            stored.ConfirmEmail(DateTimeOffset.UtcNow);

            if (trustedAuthor)
            {
                stored.GrantTrust(DateTimeOffset.UtcNow);
            }

            await db.SaveChangesAsync(cancellationToken);
        });

        return user;
    }

    private async Task<(Guid CityId, Guid CategoryId)> CreateCatalogAsync()
        => await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var ct = TestContext.Current.CancellationToken;
            var suffix = Guid.NewGuid().ToString("N");
            var section = TestData.CreateSection($"{SectionPrefix}{suffix}");
            db.Categories.Add(section);
            await db.SaveChangesAsync(ct);

            var subsection = TestData.CreateSubsection(section, $"{SubsectionPrefix}{suffix}");
            var city = TestData.CreateCity($"{CityPrefix}{suffix}");
            db.Categories.Add(subsection);
            db.Cities.Add(city);
            await db.SaveChangesAsync(ct);

            return (city.Id, subsection.Id);
        });

    private async Task<Guid> CreateDbAdAsync(
        Guid authorId,
        Guid categoryId,
        Guid cityId,
        string title,
        AdStatus status,
        DateTimeOffset publishedAt)
        => await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var ct = TestContext.Current.CancellationToken;
            var ad = TestData.CreateAd(
                authorId,
                categoryId,
                cityId,
                title,
                status: status,
                publishedAt: publishedAt);

            db.Ads.Add(ad);
            await db.SaveChangesAsync(ct);
            return ad.Id;
        });

    private async Task<HttpResponseMessage> CreateAdAsync(
        HttpClient client,
        string title,
        Guid cityId,
        Guid categoryId,
        int photoCount,
        CancellationToken cancellationToken)
    {
        using var content = BuildForm(title, cityId, categoryId);

        for (var index = 0; index < photoCount; index++)
        {
            AddPhoto(content, TestImage, "image/png", $"photo-{index}.png");
        }

        return await client.PostContentAsync("/api/ads", content, cancellationToken);
    }

    private static MultipartFormDataContent BuildForm(
        string title,
        Guid cityId,
        Guid categoryId,
        decimal price = 1000m)
        => new()
        {
            { new StringContent(title), "title" },
            { new StringContent("Описание объявления"), "description" },
            { new StringContent(price.ToString(CultureInfo.InvariantCulture)), "price" },
            { new StringContent(cityId.ToString()), "cityId" },
            { new StringContent(categoryId.ToString()), "categoryId" },
            { new StringContent("+7 900 000-00-00"), "contacts" },
        };

    private static void AddPhoto(
        MultipartFormDataContent content,
        byte[] bytes,
        string contentType,
        string fileName = "photo.png")
    {
        var photo = new ByteArrayContent(bytes);
        photo.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(photo, "photos", fileName);
    }

    private static async Task<AdPayload> ReadCreatedAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Assert.Fail($"Создание объявления вернуло {(int)response.StatusCode}: {body}");
        }

        return (await response.Content.ReadFromJsonAsync<AdPayload>(cancellationToken))!;
    }

    private static byte[] CreateTestImage()
    {
        using var bitmap = new SKBitmap(8, 8);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(new SKColor(0x00, 0xAA, 0xFF));
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>Крупное изображение с шумом: сжимается плохо и даёт файл больше 1 МБ.</summary>
    private static byte[] CreateLargeTestImage()
    {
        const int width = 1150;
        const int height = 1000;
        var random = new Random(20261121);

        using var bitmap = new SKBitmap(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                bitmap.SetPixel(
                    x,
                    y,
                    new SKColor(
                        (byte)random.Next(256),
                        (byte)random.Next(256),
                        (byte)random.Next(256)));
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private sealed record UserPayload(Guid Id, string Email, string Role, bool IsTrusted, bool IsEmailConfirmed);

    private sealed record AdPayload(
        Guid Id,
        string Title,
        decimal Price,
        string Status,
        string? ModerationReason,
        List<PhotoPayload> Photos);

    private sealed record PhotoPayload(
        Guid Id,
        string Url,
        string ThumbnailUrl,
        string ContentType,
        long ByteSize,
        int Width,
        int Height,
        int SortOrder);

    private sealed record AdPagePayload(List<AdCardPayload> Items, int TotalCount);

    private sealed record AdCardPayload(Guid Id, string Title);
}