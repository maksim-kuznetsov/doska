using System.Net;
using System.Net.Http.Json;
using Doska.Application.Abstractions;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Tests;

/// <summary>Публичная лента, поиск, фильтры, сортировка и пагинация.</summary>
public sealed class AdSearchApiTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string AdPrefix = "Поиск-тест ";
    private const string SectionPrefix = "Поиск-раздел ";
    private const string SubsectionPrefix = "Поиск-подраздел ";
    private const string CityPrefix = "Поиск-город ";

    private readonly ApiFactory _factory;

    public AdSearchApiTests(ApiFactory factory)
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

            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith("search-")));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Feed_returns_only_active_ads()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        var authorId = await CreateAuthorAsync();
        var (sectionId, subsectionId, _, cityId, _) = await CreateCatalogAsync();

        var marker = $"OnlyActive{Guid.NewGuid():N}";
        var activeId = await CreateAdAsync(
            authorId, subsectionId, cityId, $"{AdPrefix}{marker}", "Описание", 1000m, AdStatus.Active);

        foreach (var status in new[]
                 {
                     AdStatus.OnModeration, AdStatus.Rejected, AdStatus.Hidden, AdStatus.Sold, AdStatus.Archived,
                 })
        {
            await CreateAdAsync(authorId, subsectionId, cityId, $"{AdPrefix}{marker}", "Описание", 1000m, status);
        }

        using var client = _factory.CreateClient();
        var page = await client.GetFromJsonAsync<PagePayload>($"/api/ads?query={marker}", ct);

        Assert.NotNull(page);
        Assert.Equal(1, page!.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(activeId, page.Items[0].Id);
        Assert.NotEqual(Guid.Empty, sectionId);
    }

    [Fact]
    public async Task Text_search_matches_title_and_description_case_insensitively()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        var authorId = await CreateAuthorAsync();
        var (_, subsectionId, _, cityId, _) = await CreateCatalogAsync();

        var titleMarker = $"TitleMarker{Guid.NewGuid():N}";
        var descriptionMarker = $"DescriptionMarker{Guid.NewGuid():N}";

        var titleAdId = await CreateAdAsync(
            authorId, subsectionId, cityId, $"{AdPrefix}{titleMarker}", "обычное описание", 100m, AdStatus.Active);
        var descriptionAdId = await CreateAdAsync(
            authorId, subsectionId, cityId, $"{AdPrefix}обычное название", $"{AdPrefix}{descriptionMarker}", 200m, AdStatus.Active);

        using var client = _factory.CreateClient();

        var byTitle = await client.GetFromJsonAsync<PagePayload>(
            $"/api/ads?query={titleMarker.ToLowerInvariant()}",
            ct);
        var byDescription = await client.GetFromJsonAsync<PagePayload>(
            $"/api/ads?query={descriptionMarker}",
            ct);
        var withEmptyQuery = await client.GetFromJsonAsync<PagePayload>($"/api/ads?query=", ct);

        Assert.Single(byTitle!.Items);
        Assert.Equal(titleAdId, byTitle.Items[0].Id);

        Assert.Single(byDescription!.Items);
        Assert.Equal(descriptionAdId, byDescription.Items[0].Id);

        Assert.Contains(withEmptyQuery!.Items, item => item.Id == titleAdId);
    }

    [Fact]
    public async Task Section_filter_includes_its_subsections_and_subsection_filter_narrows()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        var authorId = await CreateAuthorAsync();
        var (sectionId, firstSubsectionId, secondSubsectionId, cityId, _) = await CreateCatalogAsync();

        var firstAdId = await CreateAdAsync(
            authorId, firstSubsectionId, cityId, $"{AdPrefix}Первый", "Описание", 100m, AdStatus.Active);
        var secondAdId = await CreateAdAsync(
            authorId, secondSubsectionId, cityId, $"{AdPrefix}Второй", "Описание", 200m, AdStatus.Active);

        using var client = _factory.CreateClient();

        var bySection = await client.GetFromJsonAsync<PagePayload>($"/api/ads?categoryId={sectionId}", ct);
        var bySubsection = await client.GetFromJsonAsync<PagePayload>(
            $"/api/ads?categoryId={firstSubsectionId}",
            ct);

        Assert.Equal(2, bySection!.TotalCount);
        Assert.Contains(bySection.Items, item => item.Id == firstAdId);
        Assert.Contains(bySection.Items, item => item.Id == secondAdId);

        Assert.Single(bySubsection!.Items);
        Assert.Equal(firstAdId, bySubsection.Items[0].Id);
    }

    [Fact]
    public async Task City_and_price_filters_combine_inclusively()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        var authorId = await CreateAuthorAsync();
        var (_, subsectionId, _, firstCityId, secondCityId) = await CreateCatalogAsync();

        var cheapId = await CreateAdAsync(
            authorId, subsectionId, firstCityId, $"{AdPrefix}Дешёвый", "Описание", 100m, AdStatus.Active);
        var expensiveId = await CreateAdAsync(
            authorId, subsectionId, firstCityId, $"{AdPrefix}Дорогой", "Описание", 500m, AdStatus.Active);
        var otherCityId = await CreateAdAsync(
            authorId, subsectionId, secondCityId, $"{AdPrefix}Другой город", "Описание", 500m, AdStatus.Active);

        using var client = _factory.CreateClient();

        var page = await client.GetFromJsonAsync<PagePayload>(
            $"/api/ads?cityId={firstCityId}&minPrice=100&maxPrice=500",
            ct);

        Assert.Equal(2, page!.TotalCount);
        Assert.Contains(page.Items, item => item.Id == cheapId);
        Assert.Contains(page.Items, item => item.Id == expensiveId);
        Assert.DoesNotContain(page.Items, item => item.Id == otherCityId);

        var onlyExpensive = await client.GetFromJsonAsync<PagePayload>(
            $"/api/ads?cityId={firstCityId}&minPrice=200",
            ct);

        Assert.Single(onlyExpensive!.Items);
        Assert.Equal(expensiveId, onlyExpensive.Items[0].Id);
    }

    [Fact]
    public async Task Sorting_and_pagination_work_together()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        var authorId = await CreateAuthorAsync();
        var (_, subsectionId, _, cityId, _) = await CreateCatalogAsync();

        // Даты в пределах срока жизни объявления (Ad.LifetimeDays): иначе параллельный
        // тест автоархива в ClassifiedAdsApiTests переведёт их в архив во время проверки.
        var now = DateTimeOffset.UtcNow;
        var oldest = now.AddDays(-3);
        var middle = now.AddDays(-2);
        var newest = now.AddDays(-1);

        await CreateAdAsync(authorId, subsectionId, cityId, $"{AdPrefix}Триста", "Описание", 300m, AdStatus.Active, oldest);
        await CreateAdAsync(authorId, subsectionId, cityId, $"{AdPrefix}Сто", "Описание", 100m, AdStatus.Active, middle);
        await CreateAdAsync(authorId, subsectionId, cityId, $"{AdPrefix}Двести", "Описание", 200m, AdStatus.Active, newest);

        using var client = _factory.CreateClient();

        var firstPage = await client.GetFromJsonAsync<PagePayload>(
            $"/api/ads?categoryId={subsectionId}&sort=price_asc&pageSize=2&page=1",
            ct);
        var secondPage = await client.GetFromJsonAsync<PagePayload>(
            $"/api/ads?categoryId={subsectionId}&sort=price_asc&pageSize=2&page=2",
            ct);
        var byDefault = await client.GetFromJsonAsync<PagePayload>(
            $"/api/ads?categoryId={subsectionId}&pageSize=10",
            ct);
        var descending = await client.GetFromJsonAsync<PagePayload>(
            $"/api/ads?categoryId={subsectionId}&sort=price_desc&pageSize=10",
            ct);

        Assert.Equal(3, firstPage!.TotalCount);
        Assert.Equal(new[] { 100m, 200m }, firstPage.Items.Select(item => item.Price).ToArray());
        Assert.Equal(new[] { 300m }, secondPage!.Items.Select(item => item.Price).ToArray());

        Assert.Equal(new[] { 200m, 100m, 300m }, byDefault!.Items.Select(item => item.Price).ToArray());
        Assert.Equal(new[] { 300m, 200m, 100m }, descending!.Items.Select(item => item.Price).ToArray());
    }

    [Fact]
    public async Task Invalid_price_range_returns_bad_request()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/ads?minPrice=100&maxPrice=50", ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Catalog_sections_returns_sections_with_subsections()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        var (sectionId, _, _, _, _) = await CreateCatalogAsync();

        using var client = _factory.CreateClient();
        var sections = await client.GetFromJsonAsync<List<SectionPayload>>("/api/catalog/sections", ct);

        var section = Assert.Single(sections!, candidate => candidate.Id == sectionId);
        Assert.StartsWith(SectionPrefix, section.Name);
        Assert.Equal(2, section.Subsections.Count);
    }

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private async Task<Guid> CreateAuthorAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        return await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var passwordHasher = provider.GetRequiredService<IPasswordHasher>();

            var author = new User
            {
                Email = $"search-{Guid.NewGuid():N}@example.com",
                PasswordHash = passwordHasher.Hash("secret123"),
                Role = UserRole.User,
                IsEmailConfirmed = true,
            };

            db.Users.Add(author);
            await db.SaveChangesAsync(ct);
            return author.Id;
        });
    }

    private async Task<(Guid SectionId, Guid FirstSubsectionId, Guid SecondSubsectionId, Guid FirstCityId, Guid SecondCityId)>
        CreateCatalogAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        return await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var suffix = Guid.NewGuid().ToString("N");
            var section = new Category { Name = $"{SectionPrefix}{suffix}" };
            db.Categories.Add(section);
            await db.SaveChangesAsync(ct);

            var firstSubsection = new Category { Name = $"{SubsectionPrefix}A-{suffix}", ParentId = section.Id };
            var secondSubsection = new Category { Name = $"{SubsectionPrefix}B-{suffix}", ParentId = section.Id };
            var firstCity = new City { Name = $"{CityPrefix}1-{suffix}" };
            var secondCity = new City { Name = $"{CityPrefix}2-{suffix}" };

            db.Categories.AddRange(firstSubsection, secondSubsection);
            db.Cities.AddRange(firstCity, secondCity);
            await db.SaveChangesAsync(ct);

            return (section.Id, firstSubsection.Id, secondSubsection.Id, firstCity.Id, secondCity.Id);
        });
    }

    private async Task<Guid> CreateAdAsync(
        Guid authorId,
        Guid categoryId,
        Guid cityId,
        string title,
        string description,
        decimal price,
        AdStatus status,
        DateTimeOffset? publishedAt = null)
    {
        var ct = TestContext.Current.CancellationToken;

        return await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var ad = new Ad
            {
                AuthorId = authorId,
                CategoryId = categoryId,
                CityId = cityId,
                Title = title,
                Description = description,
                Price = price,
                Contacts = "+7 900 000-00-00",
                Status = status,
                PublishedAt = publishedAt ?? DateTimeOffset.UtcNow,
            };

            db.Ads.Add(ad);
            await db.SaveChangesAsync(ct);
            return ad.Id;
        });
    }

    private sealed record PagePayload(List<CardPayload> Items, int TotalCount, int Page, int PageSize);

    private sealed record CardPayload(
        Guid Id,
        string Title,
        decimal Price,
        string CityName,
        string CategoryName,
        string SectionName,
        DateTimeOffset PublishedAt);

    private sealed record SectionPayload(Guid Id, string Name, List<SubsectionPayload> Subsections);

    private sealed record SubsectionPayload(Guid Id, string Name);
}