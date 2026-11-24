using Doska.Application.Abstractions;
using Doska.Application.Ads;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Tests;

/// <summary>Напоминание автору за сутки до автоархива объявления.</summary>
public sealed class ExpiryReminderTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string Password = "secret123";
    private const string AdPrefix = "Напом-объявление ";
    private const string CityPrefix = "Напом-город ";
    private const string CategoryPrefix = "Напом-подраздел ";

    private readonly ApiFactory _factory;

    public ExpiryReminderTests(ApiFactory factory)
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

            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith("reminder-")));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Reminder_is_sent_within_one_day_of_archive()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        _factory.Emails.Clear();

        var marker = $"Reminder{Guid.NewGuid():N}";
        var adId = await CreateActiveAdAsync($"{AdPrefix}{marker}", DateTimeOffset.UtcNow.AddDays(-6.5));

        var sent = await RunRemindersAsync(ct);

        Assert.True(sent >= 1);
        Assert.Equal(1, CountEmails(marker));

        var reminderSentAt = await TestDatabase.WithContextAsync(_factory, db => db.Ads
            .Where(ad => ad.Id == adId)
            .Select(ad => ad.ReminderSentAt)
            .FirstAsync(ct));

        Assert.NotNull(reminderSentAt);
    }

    [Fact]
    public async Task Reminder_is_not_sent_outside_the_window()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        _factory.Emails.Clear();

        var marker = $"Reminder{Guid.NewGuid():N}";
        await CreateActiveAdAsync($"{AdPrefix}{marker}", DateTimeOffset.UtcNow.AddDays(-1));

        await RunRemindersAsync(ct);

        Assert.Equal(0, CountEmails(marker));
    }

    [Fact]
    public async Task Reminder_is_sent_only_once_per_publication()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        _factory.Emails.Clear();

        var marker = $"Reminder{Guid.NewGuid():N}";
        await CreateActiveAdAsync($"{AdPrefix}{marker}", DateTimeOffset.UtcNow.AddDays(-6.5));

        await RunRemindersAsync(ct);
        await RunRemindersAsync(ct);

        Assert.Equal(1, CountEmails(marker));
    }

    [Fact]
    public async Task Renewal_resets_reminder_for_the_new_term()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        _factory.Emails.Clear();

        var marker = $"Reminder{Guid.NewGuid():N}";
        var adId = await CreateActiveAdAsync($"{AdPrefix}{marker}", DateTimeOffset.UtcNow.AddDays(-6.5));

        await RunRemindersAsync(ct);
        Assert.Equal(1, CountEmails(marker));

        // Продление: срок обновляется, напоминание считается по новому сроку.
        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var ad = await db.Ads.FirstAsync(candidate => candidate.Id == adId, ct);

            // Продление на срок, который снова подходит к границе напоминания.
            ad.Renew(DateTimeOffset.UtcNow.AddDays(-6.5));
            await db.SaveChangesAsync(ct);
        });

        await RunRemindersAsync(ct);

        Assert.Equal(2, CountEmails(marker));
    }

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private int CountEmails(string marker)
        => _factory.Emails.Messages.Count(message => message.Body.Contains(marker));

    private Task<int> RunRemindersAsync(CancellationToken cancellationToken)
        => TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var adService = provider.GetRequiredService<AdService>();
            return await adService.SendExpiryRemindersAsync(cancellationToken);
        });

    private async Task<Guid> CreateActiveAdAsync(string title, DateTimeOffset publishedAt)
        => await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var ct = TestContext.Current.CancellationToken;
            var db = provider.GetRequiredService<AppDbContext>();
            var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
            var suffix = Guid.NewGuid().ToString("N");

            var section = TestData.CreateSection($"{CategoryPrefix}{suffix}");
            db.Categories.Add(section);
            await db.SaveChangesAsync(ct);

            var subsection = TestData.CreateSubsection(section, $"{CategoryPrefix}под-{suffix}");
            var city = TestData.CreateCity($"{CityPrefix}{suffix}");
            var author = TestData.CreateUser(
                $"reminder-{suffix}@example.com",
                passwordHash: passwordHasher.Hash(Password));

            db.Categories.Add(subsection);
            db.Cities.Add(city);
            db.Users.Add(author);
            await db.SaveChangesAsync(ct);

            var ad = TestData.CreateAd(
                author.Id,
                subsection.Id,
                city.Id,
                title,
                publishedAt: publishedAt);

            db.Ads.Add(ad);
            await db.SaveChangesAsync(ct);
            return ad.Id;
        });
}