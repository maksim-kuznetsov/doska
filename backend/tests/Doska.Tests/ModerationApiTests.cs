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

/// <summary>Сквозные сценарии модерации, жалоб и доверия через Web API.</summary>
public sealed class ModerationApiTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string Password = "secret123";
    private const string TestAdPrefix = "Тестовое объявление ";
    private const string TestSectionPrefix = "Раздел ";
    private const string TestSubsectionPrefix = "Подраздел ";
    private const string TestCityPrefix = "Город ";

    private static readonly string[] TestUserPrefixes =
    [
        "author-",
        "plain-",
        "trusted-",
        "seller-",
        "reporter-",
        "manual-",
        "moderator-",
        "admin-",
    ];

    private readonly ApiFactory _factory;

    public ModerationApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Тесты не требуют подготовки.</summary>
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    /// <summary>Удаляет данные, созданные тестом, чтобы не засорять локальную базу.</summary>
    public async ValueTask DisposeAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        if (!await TestDatabase.IsReachableAsync(_factory, ct))
        {
            return;
        }

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            db.AdComplaints.RemoveRange(
                db.AdComplaints.Where(complaint => complaint.Ad!.Title.StartsWith(TestAdPrefix)));
            db.Ads.RemoveRange(db.Ads.Where(ad => ad.Title.StartsWith(TestAdPrefix)));
            await db.SaveChangesAsync(ct);

            db.Categories.RemoveRange(
                db.Categories.Where(category => category.Name.StartsWith(TestSubsectionPrefix)));
            db.Categories.RemoveRange(
                db.Categories.Where(category => category.Name.StartsWith(TestSectionPrefix)));
            db.Cities.RemoveRange(db.Cities.Where(city => city.Name.StartsWith(TestCityPrefix)));
            await db.SaveChangesAsync(ct);

            foreach (var prefix in TestUserPrefixes)
            {
                db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith(prefix)));
            }

            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Anonymous_cannot_open_moderation_queue()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/moderation/queue", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Regular_user_is_forbidden_in_moderation_queue()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        await RegisterAsync(client, UniqueEmail("plain"), ct);

        var response = await client.GetAsync("/api/moderation/queue", ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_publishes_ad_and_third_approval_grants_trust()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        var authorEmail = UniqueEmail("author");
        var authorUser = await RegisterAsync(author, authorEmail, ct);
        await ConfirmEmailAsync(authorEmail);

        using var moderator = _factory.CreateClient();
        await LoginAsync(moderator, await CreateStaffAsync(UserRole.Moderator), ct);

        for (var index = 0; index < User.TrustThreshold; index++)
        {
            var ad = await CreateAdAsync(author, ct);
            Assert.Equal("OnModeration", ad.Status);

            if (index == 0)
            {
                var queue = await moderator.GetFromJsonAsync<List<QueuePayload>>("/api/moderation/queue", ct);
                Assert.Contains(queue!, item => item.Id == ad.Id);
            }

            var response = await moderator.PostJsonAsync($"/api/moderation/ads/{ad.Id}/approve", null, ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var decision = await response.Content.ReadFromJsonAsync<DecisionPayload>(ct);
            Assert.Equal("Active", decision!.Status);
        }

        var trusted = await TestDatabase.WithContextAsync(
            _factory,
            db => db.Users.Where(user => user.Id == authorUser.Id).Select(user => user.IsTrusted).FirstAsync(ct));

        Assert.True(trusted);

        var publishedImmediately = await CreateAdAsync(author, ct);
        Assert.Equal("Active", publishedImmediately.Status);
    }

    [Fact]
    public async Task Reject_requires_reason_and_notifies_author()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        _factory.Emails.Clear();

        using var author = _factory.CreateClient();
        var authorEmail = UniqueEmail("author");
        await RegisterAsync(author, authorEmail, ct);
        await ConfirmEmailAsync(authorEmail);

        using var moderator = _factory.CreateClient();
        await LoginAsync(moderator, await CreateStaffAsync(UserRole.Moderator), ct);

        var ad = await CreateAdAsync(author, ct);

        var withoutReason = await moderator.PostJsonAsync(
            $"/api/moderation/ads/{ad.Id}/reject",
            new { reason = "   " },
            ct);
        Assert.Equal(HttpStatusCode.BadRequest, withoutReason.StatusCode);

        var rejected = await moderator.PostJsonAsync(
            $"/api/moderation/ads/{ad.Id}/reject",
            new { reason = "Не по теме раздела" },
            ct);
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);

        var stored = await TestDatabase.WithContextAsync(
            _factory,
            db => db.Ads.Where(candidate => candidate.Id == ad.Id)
                .Select(candidate => new { candidate.Status, candidate.ModerationReason })
                .FirstAsync(ct));

        Assert.Equal(AdStatus.Rejected, stored.Status);
        Assert.Equal("Не по теме раздела", stored.ModerationReason);
        Assert.Contains(
            _factory.Emails.Messages,
            message => message.Email == authorEmail && message.Subject.Contains("отклонено"));
    }

    [Fact]
    public async Task Hide_hides_active_ad_and_notifies_author()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        _factory.Emails.Clear();

        using var author = _factory.CreateClient();
        var authorEmail = UniqueEmail("trusted");
        await RegisterAsync(author, authorEmail, ct);
        await ConfirmEmailAsync(authorEmail);
        await SetTrustAsync(authorEmail, true);

        using var moderator = _factory.CreateClient();
        await LoginAsync(moderator, await CreateStaffAsync(UserRole.Moderator), ct);

        var ad = await CreateAdAsync(author, ct);
        Assert.Equal("Active", ad.Status);

        var withoutReason = await moderator.PostJsonAsync(
            $"/api/moderation/ads/{ad.Id}/hide",
            new { reason = string.Empty },
            ct);
        Assert.Equal(HttpStatusCode.BadRequest, withoutReason.StatusCode);

        var hidden = await moderator.PostJsonAsync(
            $"/api/moderation/ads/{ad.Id}/hide",
            new { reason = "Нарушение правил" },
            ct);
        Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);

        var status = await TestDatabase.WithContextAsync(
            _factory,
            db => db.Ads.Where(candidate => candidate.Id == ad.Id).Select(candidate => candidate.Status).FirstAsync(ct));

        Assert.Equal(AdStatus.Hidden, status);
        Assert.Contains(
            _factory.Emails.Messages,
            message => message.Email == authorEmail && message.Subject.Contains("скрыто"));
    }

    [Fact]
    public async Task Complaints_prevent_duplicates_and_can_hide_ad()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        var authorEmail = UniqueEmail("seller");
        await RegisterAsync(author, authorEmail, ct);
        await ConfirmEmailAsync(authorEmail);
        await SetTrustAsync(authorEmail, true);

        var ad = await CreateAdAsync(author, ct);
        Assert.Equal("Active", ad.Status);

        using var reporter = _factory.CreateClient();
        var reporterEmail = UniqueEmail("reporter");
        await RegisterAsync(reporter, reporterEmail, ct);
        await ConfirmEmailAsync(reporterEmail);

        var filed = await reporter.PostJsonAsync("/api/complaints", new { adId = ad.Id, reason = "Подозрительно" }, ct);
        Assert.Equal(HttpStatusCode.NoContent, filed.StatusCode);

        var duplicate = await reporter.PostJsonAsync("/api/complaints", new { adId = ad.Id, reason = "Ещё раз" }, ct);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var ownAd = await author.PostJsonAsync("/api/complaints", new { adId = ad.Id, reason = "Своё" }, ct);
        Assert.Equal(HttpStatusCode.Forbidden, ownAd.StatusCode);

        using var moderator = _factory.CreateClient();
        await LoginAsync(moderator, await CreateStaffAsync(UserRole.Moderator), ct);

        var queue = await moderator.GetFromJsonAsync<List<ComplaintPayload>>("/api/complaints/queue", ct);
        var complaint = Assert.Single(queue!, item => item.AdId == ad.Id);

        var resolved = await moderator.PostJsonAsync(
            $"/api/complaints/{complaint.Id}/resolve",
            new { hideAd = true, hideReason = "Правила нарушены" },
            ct);
        Assert.Equal(HttpStatusCode.NoContent, resolved.StatusCode);

        var status = await TestDatabase.WithContextAsync(
            _factory,
            db => db.Ads.Where(candidate => candidate.Id == ad.Id).Select(candidate => candidate.Status).FirstAsync(ct));
        Assert.Equal(AdStatus.Hidden, status);

        var again = await moderator.PostJsonAsync(
            $"/api/complaints/{complaint.Id}/resolve",
            new { hideAd = false },
            ct);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Manual_trust_grant_and_revoke_changes_publication()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var author = _factory.CreateClient();
        var authorEmail = UniqueEmail("manual");
        var authorUser = await RegisterAsync(author, authorEmail, ct);
        await ConfirmEmailAsync(authorEmail);

        using var moderator = _factory.CreateClient();
        await LoginAsync(moderator, await CreateStaffAsync(UserRole.Moderator), ct);

        var grant = await moderator.PostJsonAsync($"/api/users/{authorUser.Id}/trust", null, ct);
        Assert.Equal(HttpStatusCode.OK, grant.StatusCode);

        var published = await CreateAdAsync(author, ct);
        Assert.Equal("Active", published.Status);

        var revoke = await moderator.DeleteWithCsrfAsync($"/api/users/{authorUser.Id}/trust", ct);
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        var onModeration = await CreateAdAsync(author, ct);
        Assert.Equal("OnModeration", onModeration.Status);
    }

    private async Task EnsureDatabaseAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        if (!await TestDatabase.IsReachableAsync(_factory, ct))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.com";

    private async Task<UserPayload> RegisterAsync(HttpClient client, string email, CancellationToken cancellationToken)
    {
        var response = await client.PostJsonAsync(
            "/api/auth/register",
            new { email, password = Password },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserPayload>(cancellationToken))!;
    }

    private async Task LoginAsync(HttpClient client, string email, CancellationToken cancellationToken)
    {
        var response = await client.PostJsonAsync(
            "/api/auth/login",
            new { email, password = Password },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task ConfirmEmailAsync(string email)
    {
        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var user = await db.Users.FirstAsync(candidate => candidate.Email == email);
            user.IsEmailConfirmed = true;
            await db.SaveChangesAsync();
        });
    }

    private async Task SetTrustAsync(string email, bool trusted)
    {
        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var user = await db.Users.FirstAsync(candidate => candidate.Email == email);

            if (trusted)
            {
                user.GrantTrust();
            }
            else
            {
                user.RevokeTrust();
            }

            await db.SaveChangesAsync();
        });
    }

    private async Task<string> CreateStaffAsync(UserRole role)
    {
        var email = UniqueEmail(role.ToString().ToLowerInvariant());

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

    private async Task<(Guid CityId, Guid CategoryId)> EnsureCatalogAsync()
        => await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var suffix = Guid.NewGuid().ToString("N");
            var section = new Category { Name = $"Раздел {suffix}" };
            db.Categories.Add(section);
            await db.SaveChangesAsync();

            var subsection = new Category { Name = $"Подраздел {suffix}", ParentId = section.Id };
            var city = new City { Name = $"Город {suffix}" };
            db.Categories.Add(subsection);
            db.Cities.Add(city);
            await db.SaveChangesAsync();

            return (city.Id, subsection.Id);
        });

    private async Task<AdPayload> CreateAdAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var (cityId, categoryId) = await EnsureCatalogAsync();

        var response = await client.PostJsonAsync(
            "/api/ads",
            new
            {
                title = $"Тестовое объявление {Guid.NewGuid():N}",
                description = "Описание объявления",
                price = 1000m,
                cityId,
                categoryId,
                contacts = "+7 900 111-22-33",
            },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AdPayload>(cancellationToken))!;
    }

    private sealed record UserPayload(Guid Id, string Email, string Role, bool IsTrusted, bool IsEmailConfirmed);

    private sealed record AdPayload(Guid Id, string Status, string? ModerationReason);

    private sealed record QueuePayload(Guid Id, string Title, string AuthorEmail, DateTimeOffset SubmittedAt);

    private sealed record DecisionPayload(Guid AdId, string Status, bool TrustGranted);

    private sealed record ComplaintPayload(Guid Id, Guid AdId, string AdTitle, string AuthorEmail, string Reason, DateTimeOffset CreatedAt);
}
