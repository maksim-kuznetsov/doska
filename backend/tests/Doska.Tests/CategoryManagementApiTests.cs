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

/// <summary>Управление категориями администратором и публичное дерево.</summary>
public sealed class CategoryManagementApiTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string Password = "secret123";
    private const string SectionPrefix = "Кат-раздел ";
    private const string SubsectionPrefix = "Кат-подраздел ";
    private const string CityPrefix = "Кат-город ";
    private const string AdPrefix = "Кат-объявление ";

    private readonly ApiFactory _factory;

    public CategoryManagementApiTests(ApiFactory factory)
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

            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith("cat-")));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Anonymous_cannot_manage_categories()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/admin/categories",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Moderator_and_user_cannot_manage_categories()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var moderator = _factory.CreateClient();
        await LoginAsync(moderator, await CreateStaffAsync(UserRole.Moderator), ct);

        var moderatorTree = await moderator.GetAsync("/api/admin/categories", ct);
        Assert.Equal(HttpStatusCode.Forbidden, moderatorTree.StatusCode);

        var moderatorCreate = await moderator.PostJsonAsync(
            "/api/admin/categories/sections",
            new { name = Unique(SectionPrefix), sortOrder = 1 },
            ct);
        Assert.Equal(HttpStatusCode.Forbidden, moderatorCreate.StatusCode);

        using var user = _factory.CreateClient();
        await LoginAsync(user, await CreateStaffAsync(UserRole.User), ct);

        var userTree = await user.GetAsync("/api/admin/categories", ct);
        Assert.Equal(HttpStatusCode.Forbidden, userTree.StatusCode);
    }

    [Fact]
    public async Task Admin_creates_section_and_subsection_and_public_tree_shows_them()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var section = await CreateSectionAsync(admin, 1, ct);
        var subsectionName = Unique(SubsectionPrefix);
        var subsectionResponse = await admin.PostJsonAsync(
            $"/api/admin/categories/{section.Id}/subsections",
            new { name = subsectionName, sortOrder = 2 },
            ct);

        Assert.Equal(HttpStatusCode.Created, subsectionResponse.StatusCode);
        var subsection = await subsectionResponse.Content.ReadFromJsonAsync<ItemPayload>(ct);

        var adminTree = await admin.GetFromJsonAsync<List<SectionPayload>>("/api/admin/categories", ct);
        var adminSection = Assert.Single(adminTree!, candidate => candidate.Id == section.Id);
        Assert.Contains(adminSection.Subsections, candidate => candidate.Id == subsection!.Id);

        using var anonymous = _factory.CreateClient();
        var publicSections = await anonymous.GetFromJsonAsync<List<SectionPayload>>("/api/catalog/sections", ct);
        var publicSection = Assert.Single(publicSections!, candidate => candidate.Id == section.Id);
        Assert.Contains(publicSection.Subsections, candidate => candidate.Id == subsection!.Id);
    }

    [Fact]
    public async Task Third_level_is_rejected()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var section = await CreateSectionAsync(admin, 1, ct);
        var subsection = await CreateSubsectionAsync(admin, section.Id, 1, ct);

        var response = await admin.PostJsonAsync(
            $"/api/admin/categories/{subsection.Id}/subsections",
            new { name = Unique(SubsectionPrefix), sortOrder = 1 },
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_name_returns_conflict()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var section = await CreateSectionAsync(admin, 1, ct);

        var duplicate = await admin.PostJsonAsync(
            "/api/admin/categories/sections",
            new { name = section.Name, sortOrder = 2 },
            ct);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Update_renames_and_reorders_category()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var section = await CreateSectionAsync(admin, 1, ct);
        var newName = Unique(SectionPrefix);

        var update = await admin.PutJsonAsync(
            $"/api/admin/categories/{section.Id}",
            new { name = newName, sortOrder = 7 },
            ct);

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<ItemPayload>(ct);
        Assert.Equal(newName, updated!.Name);
        Assert.Equal(7, updated.SortOrder);

        var invalidOrder = await admin.PutJsonAsync(
            $"/api/admin/categories/{section.Id}",
            new { name = newName, sortOrder = -1 },
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, invalidOrder.StatusCode);
    }

    [Fact]
    public async Task Deactivation_hides_subsection_from_public_tree_but_keeps_ads_available()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var section = await CreateSectionAsync(admin, 1, ct);
        var subsection = await CreateSubsectionAsync(admin, section.Id, 1, ct);

        var marker = $"CategoryMarker{Guid.NewGuid():N}";
        await CreateActiveAdAsync(subsection.Id, marker);

        using var anonymous = _factory.CreateClient();

        var before = await anonymous.GetFromJsonAsync<AdPagePayload>($"/api/ads?query={marker}", ct);
        Assert.Single(before!.Items);

        var deactivate = await admin.PostJsonAsync(
            $"/api/admin/categories/{subsection.Id}/deactivate",
            null,
            ct);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var publicSections = await anonymous.GetFromJsonAsync<List<SectionPayload>>("/api/catalog/sections", ct);
        var publicSection = Assert.Single(publicSections!, candidate => candidate.Id == section.Id);
        Assert.DoesNotContain(publicSection.Subsections, candidate => candidate.Id == subsection.Id);

        var afterDeactivation = await anonymous.GetFromJsonAsync<AdPagePayload>($"/api/ads?query={marker}", ct);
        Assert.Single(afterDeactivation!.Items);

        var directSubsection = await anonymous.GetFromJsonAsync<AdPagePayload>(
            $"/api/ads?categoryId={subsection.Id}",
            ct);
        Assert.Single(directSubsection!.Items);

        var activate = await admin.PostJsonAsync(
            $"/api/admin/categories/{subsection.Id}/activate",
            null,
            ct);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var restored = await anonymous.GetFromJsonAsync<List<SectionPayload>>("/api/catalog/sections", ct);
        var restoredSection = Assert.Single(restored!, candidate => candidate.Id == section.Id);
        Assert.Contains(restoredSection.Subsections, candidate => candidate.Id == subsection.Id);
    }

    [Fact]
    public async Task Deleting_subsection_with_ads_is_rejected()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var section = await CreateSectionAsync(admin, 1, ct);
        var subsection = await CreateSubsectionAsync(admin, section.Id, 1, ct);
        await CreateActiveAdAsync(subsection.Id, $"DeleteGuard{Guid.NewGuid():N}");

        var response = await admin.DeleteWithCsrfAsync(
            $"/api/admin/categories/{subsection.Id}",
            ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_section_with_subsections_is_rejected()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var section = await CreateSectionAsync(admin, 1, ct);
        await CreateSubsectionAsync(admin, section.Id, 1, ct);

        var response = await admin.DeleteWithCsrfAsync(
            $"/api/admin/categories/{section.Id}",
            ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_empty_subsection_succeeds()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var section = await CreateSectionAsync(admin, 1, ct);
        var subsection = await CreateSubsectionAsync(admin, section.Id, 1, ct);

        var response = await admin.DeleteWithCsrfAsync(
            $"/api/admin/categories/{subsection.Id}",
            ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var tree = await admin.GetFromJsonAsync<List<SectionPayload>>("/api/admin/categories", ct);
        var stored = Assert.Single(tree!, candidate => candidate.Id == section.Id);
        Assert.DoesNotContain(stored.Subsections, candidate => candidate.Id == subsection.Id);
    }

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private static string Unique(string prefix) => $"{prefix}{Guid.NewGuid():N}";

    private async Task<SectionPayload> CreateSectionAsync(
        HttpClient client,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var response = await client.PostJsonAsync(
            "/api/admin/categories/sections",
            new { name = Unique(SectionPrefix), sortOrder },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SectionPayload>(cancellationToken))!;
    }

    private async Task<ItemPayload> CreateSubsectionAsync(
        HttpClient client,
        Guid sectionId,
        int sortOrder,
        CancellationToken cancellationToken)
    {
        var response = await client.PostJsonAsync(
            $"/api/admin/categories/{sectionId}/subsections",
            new { name = Unique(SubsectionPrefix), sortOrder },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ItemPayload>(cancellationToken))!;
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
        var email = $"cat-{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

        await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var passwordHasher = provider.GetRequiredService<IPasswordHasher>();

            db.Users.Add(TestData.CreateUser(
                email,
                role,
                passwordHash: passwordHasher.Hash(Password)));

            await db.SaveChangesAsync();
            return 0;
        });

        return email;
    }

    private async Task CreateActiveAdAsync(Guid categoryId, string marker)
    {
        var ct = TestContext.Current.CancellationToken;

        await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
            var suffix = Guid.NewGuid().ToString("N");

            var author = TestData.CreateUser(
                $"cat-author-{suffix}@example.com",
                passwordHash: passwordHasher.Hash(Password));
            var city = TestData.CreateCity($"{CityPrefix}{suffix}");

            db.Users.Add(author);
            db.Cities.Add(city);
            await db.SaveChangesAsync(ct);

            db.Ads.Add(TestData.CreateAd(
                author.Id,
                categoryId,
                city.Id,
                $"{AdPrefix}{marker}"));

            await db.SaveChangesAsync(ct);
            return 0;
        });
    }

    private sealed record SectionPayload(
        Guid Id,
        string Name,
        int SortOrder,
        bool IsActive,
        List<ItemPayload> Subsections);

    private sealed record ItemPayload(Guid Id, string Name, int SortOrder, bool IsActive);

    private sealed record AdPagePayload(List<AdCardPayload> Items, int TotalCount);

    private sealed record AdCardPayload(Guid Id, string Title);
}