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

/// <summary>Управление справочником городов администратором.</summary>
public sealed class CityManagementApiTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string Password = "secret123";
    private const string CityPrefix = "Город-";
    private const string AdPrefix = "Город-объявление ";
    private const string CategoryPrefix = "Город-подраздел ";

    private readonly ApiFactory _factory;

    public CityManagementApiTests(ApiFactory factory)
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

            db.Categories.RemoveRange(db.Categories.Where(category => category.Name.StartsWith(CategoryPrefix)));
            await db.SaveChangesAsync(ct);

            db.Cities.RemoveRange(db.Cities.Where(city => city.Name.StartsWith(CityPrefix)));
            await db.SaveChangesAsync(ct);

            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith("city-")));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Anonymous_gets_unauthorized_and_staff_gets_forbidden()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var anonymous = _factory.CreateClient();
        var anonymousList = await anonymous.GetAsync("/api/admin/cities", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousList.StatusCode);

        using var moderator = _factory.CreateClient();
        await LoginAsync(moderator, await CreateStaffAsync(UserRole.Moderator), ct);
        Assert.Equal(HttpStatusCode.Forbidden, (await moderator.GetAsync("/api/admin/cities", ct)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await moderator.PostJsonAsync("/api/admin/cities", new { name = Unique() }, ct)).StatusCode);

        using var user = _factory.CreateClient();
        await LoginAsync(user, await CreateStaffAsync(UserRole.User), ct);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/admin/cities", ct)).StatusCode);
    }

    [Fact]
    public async Task Admin_creates_city_and_it_appears_in_public_list()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var name = Unique();
        var created = await admin.PostJsonAsync("/api/admin/cities", new { name }, ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var city = await created.Content.ReadFromJsonAsync<CityPayload>(ct);
        Assert.Equal(name, city!.Name);
        Assert.True(city.IsActive);

        var adminList = await admin.GetFromJsonAsync<List<CityPayload>>("/api/admin/cities", ct);
        Assert.Contains(adminList!, candidate => candidate.Id == city.Id);

        using var anonymous = _factory.CreateClient();
        var publicList = await anonymous.GetFromJsonAsync<List<PublicCityPayload>>("/api/catalog/cities", ct);
        Assert.Contains(publicList!, candidate => candidate.Id == city.Id && candidate.Name == name);
    }

    [Fact]
    public async Task Rename_updates_name_and_duplicate_is_rejected()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var city = await CreateCityAsync(admin, ct);
        var newName = Unique();

        var renamed = await admin.PutJsonAsync($"/api/admin/cities/{city.Id}", new { name = newName }, ct);
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var updated = await renamed.Content.ReadFromJsonAsync<CityPayload>(ct);
        Assert.Equal(newName, updated!.Name);

        var other = await CreateCityAsync(admin, ct);
        var duplicate = await admin.PutJsonAsync(
            $"/api/admin/cities/{other.Id}",
            new { name = newName },
            ct);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var empty = await admin.PutJsonAsync($"/api/admin/cities/{other.Id}", new { name = "  " }, ct);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task Deactivation_hides_city_from_selection_but_keeps_ads_available()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var city = await CreateCityAsync(admin, ct);
        var marker = $"CityMarker{Guid.NewGuid():N}";
        await CreateActiveAdAsync(city.Id, marker);

        using var anonymous = _factory.CreateClient();

        var deactivate = await admin.PostJsonAsync($"/api/admin/cities/{city.Id}/deactivate", null, ct);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var publicList = await anonymous.GetFromJsonAsync<List<PublicCityPayload>>("/api/catalog/cities", ct);
        Assert.DoesNotContain(publicList!, candidate => candidate.Id == city.Id);

        var feed = await anonymous.GetFromJsonAsync<AdPagePayload>($"/api/ads?query={marker}", ct);
        Assert.Single(feed!.Items);

        var activate = await admin.PostJsonAsync($"/api/admin/cities/{city.Id}/activate", null, ct);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var restored = await anonymous.GetFromJsonAsync<List<PublicCityPayload>>("/api/catalog/cities", ct);
        Assert.Contains(restored!, candidate => candidate.Id == city.Id);
    }

    [Fact]
    public async Task Deleting_city_with_ads_is_rejected_and_empty_city_is_removed()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var busyCity = await CreateCityAsync(admin, ct);
        await CreateActiveAdAsync(busyCity.Id, $"DeleteGuard{Guid.NewGuid():N}");

        var rejected = await admin.DeleteWithCsrfAsync($"/api/admin/cities/{busyCity.Id}", ct);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);

        var emptyCity = await CreateCityAsync(admin, ct);
        var removed = await admin.DeleteWithCsrfAsync($"/api/admin/cities/{emptyCity.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        var list = await admin.GetFromJsonAsync<List<CityPayload>>("/api/admin/cities", ct);
        Assert.DoesNotContain(list!, candidate => candidate.Id == emptyCity.Id);
    }

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private static string Unique() => $"{CityPrefix}{Guid.NewGuid():N}";

    private async Task<CityPayload> CreateCityAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response = await client.PostJsonAsync("/api/admin/cities", new { name = Unique() }, cancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CityPayload>(cancellationToken))!;
    }

    private async Task LoginAsync(HttpClient client, string email, CancellationToken cancellationToken)
    {
        var response = await client.PostJsonAsync(
            "/api/auth/login",
            new { email, password = Password },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> CreateStaffAsync(UserRole role)
    {
        var email = $"city-{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

        await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var passwordHasher = provider.GetRequiredService<IPasswordHasher>();

            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = passwordHasher.Hash(Password),
                Role = role,
                IsEmailConfirmed = true,
            });

            await db.SaveChangesAsync();
            return 0;
        });

        return email;
    }

    private async Task CreateActiveAdAsync(Guid cityId, string marker)
    {
        var ct = TestContext.Current.CancellationToken;

        await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
            var suffix = Guid.NewGuid().ToString("N");

            var author = new User
            {
                Email = $"city-author-{suffix}@example.com",
                PasswordHash = passwordHasher.Hash(Password),
                Role = UserRole.User,
                IsEmailConfirmed = true,
            };
            var section = new Category { Name = $"{CategoryPrefix}{suffix}" };
            db.Categories.Add(section);
            await db.SaveChangesAsync(ct);

            var subsection = new Category { Name = $"{CategoryPrefix}под-{suffix}", ParentId = section.Id };
            db.Categories.Add(subsection);
            db.Users.Add(author);
            await db.SaveChangesAsync(ct);

            db.Ads.Add(new Ad
            {
                AuthorId = author.Id,
                CategoryId = subsection.Id,
                CityId = cityId,
                Title = $"{AdPrefix}{marker}",
                Description = "Описание",
                Price = 1000m,
                Contacts = "+7 900 000-00-00",
                Status = AdStatus.Active,
                PublishedAt = DateTimeOffset.UtcNow,
            });

            await db.SaveChangesAsync(ct);
            return 0;
        });
    }

    private sealed record CityPayload(Guid Id, string Name, bool IsActive);

    private sealed record PublicCityPayload(Guid Id, string Name);

    private sealed record AdPagePayload(List<AdCardPayload> Items, int TotalCount);

    private sealed record AdCardPayload(Guid Id, string Title);
}