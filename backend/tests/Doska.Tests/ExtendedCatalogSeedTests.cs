using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Doska.Domain.Entities;
using Doska.Infrastructure.Persistence;
using Doska.Infrastructure.Seed;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace Doska.Tests;

/// <summary>Расширенный сид справочника: данные, идемпотентность и подача в «Другой».</summary>
[Collection("Database")]
public sealed class ExtendedCatalogSeedTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string Password = "secret123";
    private const string AdPrefix = "Каталог-тест ";

    private static readonly string[] LargestCities =
        ["Череповец", "Сургут", "Назрань", "Балашиха", "Гатчина", "Новый Уренгой"];

    private static readonly byte[] TestImage = CreateTestImage();

    private readonly ApiFactory _factory;

    public ExtendedCatalogSeedTests(ApiFactory factory)
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

            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith("catalog-")));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public void Catalog_seed_data_is_consistent_and_includes_other()
    {
        Assert.True(CatalogSeedData.Cities.Length >= 90);
        Assert.Contains("Другой", CatalogSeedData.Cities);
        Assert.Equal(
            CatalogSeedData.Cities.Length,
            CatalogSeedData.Cities.Distinct(StringComparer.Ordinal).Count());

        Assert.Equal(12, CatalogSeedData.Sections.Length);

        var sectionNames = CatalogSeedData.Sections.Select(section => section.Name).ToArray();
        var subsectionNames = CatalogSeedData.Sections
            .SelectMany(section => section.Subsections)
            .ToArray();

        var allNames = sectionNames.Concat(subsectionNames).ToArray();
        Assert.Equal(allNames.Length, allNames.Distinct(StringComparer.Ordinal).Count());

        var miscellaneous = Assert.Single(CatalogSeedData.Sections, section => section.Name == "Разное");
        Assert.Equal(new[] { "Другой" }, miscellaneous.Subsections);

        foreach (var (name, subsections) in CatalogSeedData.Sections)
        {
            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.True(name.Length <= Category.MaxNameLength);
            Assert.NotEmpty(subsections);

            foreach (var subsection in subsections)
            {
                Assert.False(string.IsNullOrWhiteSpace(subsection));
                Assert.True(subsection.Length <= Category.MaxNameLength);
            }
        }
    }

    [Fact]
    public async Task Seed_is_idempotent_and_reuses_base_references()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        await TestDatabase.WithContextAsync(_factory, db => ReferenceDataSeeder.SeedAsync(db, TimeProvider.System, ct));
        await SeedAsync(ct);
        var second = await SeedAsync(ct);

        Assert.Equal(0, second.CitiesAdded);
        Assert.Equal(0, second.SectionsAdded);
        Assert.Equal(0, second.SubsectionsAdded);
        Assert.Empty(second.Skipped);

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            Assert.Equal(1, await db.Cities.CountAsync(city => city.Name == "Москва", ct));
            Assert.Equal(1, await db.Cities.CountAsync(city => city.Name == "Другой", ct));
            Assert.Equal(
                1,
                await db.Categories.CountAsync(
                    category => category.Name == "Транспорт" && category.ParentId == null,
                    ct));
            Assert.Equal(1, await db.Categories.CountAsync(category => category.Name == "Автомобили", ct));

            var other = await db.Categories
                .Include(category => category.Parent)
                .SingleAsync(category => category.Name == "Другой", ct);

            Assert.NotNull(other.Parent);
            Assert.Equal("Разное", other.Parent!.Name);

            foreach (var sectionName in CatalogSeedData.Sections.Select(section => section.Name))
            {
                var hasSubsections = await db.Categories.AnyAsync(
                    category => category.Parent!.Name == sectionName,
                    ct);

                Assert.True(hasSubsections, $"У раздела «{sectionName}» нет подразделов.");
            }

            foreach (var cityName in LargestCities)
            {
                Assert.True(
                    await db.Cities.AnyAsync(city => city.Name == cityName, ct),
                    $"Нет города «{cityName}».");
            }
        });
    }

    [Fact]
    public async Task Seeded_other_option_is_available_and_can_host_an_ad()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        await SeedAsync(ct);

        using var client = _factory.CreateClient();
        await RegisterTrustedAuthorAsync(client, ct);

        var cities = await client.GetFromJsonAsync<List<CityPayload>>("/api/catalog/cities", ct);
        var otherCity = Assert.Single(cities!, city => city.Name == "Другой");
        Assert.Contains(cities!, city => city.Name == "Череповец");

        var sections = await client.GetFromJsonAsync<List<SectionPayload>>("/api/catalog/sections", ct);
        var miscellaneous = Assert.Single(sections!, section => section.Name == "Разное");
        var otherSubsection = Assert.Single(miscellaneous.Subsections, subsection => subsection.Name == "Другой");

        var marker = $"Другой-{Guid.NewGuid():N}";

        using (var form = BuildForm($"{AdPrefix}{marker}", otherCity.Id, otherSubsection.Id))
        {
            AddPhoto(form, TestImage, "image/png");

            var created = await client.PostContentAsync("/api/ads", form, ct);

            if (created.StatusCode != HttpStatusCode.Created)
            {
                var body = await created.Content.ReadAsStringAsync(ct);
                Assert.Fail($"Создание объявления вернуло {(int)created.StatusCode}: {body}");
            }
        }

        var page = await client.GetFromJsonAsync<AdPagePayload>(
            $"/api/ads?categoryId={otherSubsection.Id}&query={marker}",
            ct);

        Assert.Single(page!.Items);
    }

    private Task<CatalogSeedResult> SeedAsync(CancellationToken cancellationToken)
        => TestDatabase.WithContextAsync(
            _factory,
            db => ExtendedCatalogSeeder.SeedAsync(db, TimeProvider.System, cancellationToken));

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private async Task RegisterTrustedAuthorAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var email = $"catalog-{Guid.NewGuid():N}@example.com";
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
            stored.GrantTrust(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        });
    }

    private static MultipartFormDataContent BuildForm(string title, Guid cityId, Guid categoryId)
        => new()
        {
            { new StringContent(title), "title" },
            { new StringContent("Описание объявления"), "description" },
            { new StringContent(1000m.ToString(CultureInfo.InvariantCulture)), "price" },
            { new StringContent(cityId.ToString()), "cityId" },
            { new StringContent(categoryId.ToString()), "categoryId" },
            { new StringContent("+7 900 000-00-00"), "contacts" },
        };

    private static void AddPhoto(MultipartFormDataContent content, byte[] bytes, string contentType)
    {
        var photo = new ByteArrayContent(bytes);
        photo.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(photo, "photos", "photo.png");
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

    private sealed record UserPayload(Guid Id, string Email);

    private sealed record CityPayload(Guid Id, string Name);

    private sealed record SectionPayload(Guid Id, string Name, List<SubsectionPayload> Subsections);

    private sealed record SubsectionPayload(Guid Id, string Name);

    private sealed record AdPagePayload(List<AdCardPayload> Items, int TotalCount);

    private sealed record AdCardPayload(Guid Id, string Title);
}
