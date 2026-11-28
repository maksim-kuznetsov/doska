using Doska.Application.Auth;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Tests;

/// <summary>Разрешение идентичности внешнего провайдера и привязка к аккаунту.</summary>
public sealed class ExternalAuthServiceTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string EmailPrefix = "external-auth-";
    private const string Google = "google";
    private const string Yandex = "yandex";

    private readonly ApiFactory _factory;

    public ExternalAuthServiceTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        if (!await TestDatabase.IsReachableAsync(_factory, ct))
        {
            return;
        }

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith(EmailPrefix)));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Unknown_identity_creates_confirmed_passwordless_account()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        const string subject = "external-subject-new";

        var authenticated = await WithServiceAsync(
            service => service.SignInAsync(Google, subject, email, null, emailVerified: true, ct));

        var user = await FindUserAsync(email, ct);
        Assert.Equal(user.Id, authenticated.Id);
        Assert.Equal(subject, await FindSubjectAsync(user.Id, Google, ct));
        Assert.True(user.IsEmailConfirmed);
        Assert.False(user.HasPassword);
        Assert.Equal(UserRole.User, user.Role);
        Assert.Contains(Google, authenticated.LinkedProviders);
    }

    [Fact]
    public async Task Existing_email_is_linked_automatically_and_confirmed()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        var user = new User(email, "hash", UserRole.User, DateTimeOffset.UtcNow);
        await AddUserAsync(user, ct);

        var authenticated = await WithServiceAsync(
            service => service.SignInAsync(Google, "external-subject-auto", email.ToUpperInvariant(), null, true, ct));

        var stored = await FindUserAsync(email, ct);
        Assert.Equal(user.Id, authenticated.Id);
        Assert.Equal("external-subject-auto", await FindSubjectAsync(user.Id, Google, ct));
        Assert.True(stored.IsEmailConfirmed);
        // При автоматической привязке прежний пароль инвалидируется,
        // чтобы исключить pre-hijacking атаку.
        Assert.False(stored.HasPassword);
        Assert.Equal(1, await CountUsersAsync(email, ct));
    }

    [Fact]
    public async Task Linked_subject_wins_over_changed_email()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        const string subject = "external-subject-stable";

        var first = await WithServiceAsync(
            service => service.SignInAsync(Google, subject, email, null, true, ct));

        var second = await WithServiceAsync(
            service => service.SignInAsync(Google, subject, UniqueEmail(), null, true, ct));

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await CountUsersAsync(email, ct));
    }

    [Fact]
    public async Task Blocked_linked_account_is_rejected()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        const string subject = "external-subject-blocked";

        await WithServiceAsync(service => service.SignInAsync(Google, subject, email, null, true, ct));

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var user = await db.Users.FirstAsync(candidate => candidate.Email == email, ct);
            user.Block(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);
        });

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            WithServiceAsync(service => service.SignInAsync(Google, subject, email, null, true, ct)));

        Assert.Equal(ServiceErrorKind.Forbidden, exception.Kind);
    }

    [Fact]
    public async Task Email_of_account_linked_to_another_subject_is_not_relinked()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        var user = await AddLinkedUserAsync(email, Google, "external-subject-first", ct);

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            WithServiceAsync(service =>
                service.SignInAsync(Google, "external-subject-second", email, null, true, ct)));

        Assert.Equal(ServiceErrorKind.Conflict, exception.Kind);
        Assert.Equal("external-subject-first", await FindSubjectAsync(user.Id, Google, ct));
    }

    [Fact]
    public async Task Unverified_email_is_rejected_without_creating_account()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            WithServiceAsync(service =>
                service.SignInAsync(Google, "external-subject-unverified", email, null, false, ct)));

        Assert.Equal(ServiceErrorKind.Validation, exception.Kind);
        Assert.Equal(0, await CountUsersAsync(email, ct));
    }

    [Fact]
    public async Task Explicit_link_keeps_account_email_and_does_not_confirm_different_email()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var accountEmail = UniqueEmail();
        var user = new User(accountEmail, "hash", UserRole.User, DateTimeOffset.UtcNow);
        await AddUserAsync(user, ct);

        var authenticated = await WithServiceAsync(
            service => service.LinkAsync(user.Id, Google, "external-subject-link", UniqueEmail(), null, true, ct));

        var stored = await FindUserAsync(accountEmail, ct);
        Assert.Equal(user.Id, authenticated.Id);
        Assert.Equal("external-subject-link", await FindSubjectAsync(user.Id, Google, ct));
        Assert.Equal(accountEmail, stored.Email);
        Assert.False(stored.IsEmailConfirmed);
    }

    [Fact]
    public async Task Explicit_link_confirms_matching_email()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        var user = new User(email, "hash", UserRole.User, DateTimeOffset.UtcNow);
        await AddUserAsync(user, ct);

        await WithServiceAsync(
            service => service.LinkAsync(user.Id, Google, "external-subject-link-match", email, null, true, ct));

        Assert.True((await FindUserAsync(email, ct)).IsEmailConfirmed);
    }

    [Fact]
    public async Task Explicit_link_rejects_identity_owned_by_another_account()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var ownerEmail = UniqueEmail();
        var owner = await AddLinkedUserAsync(ownerEmail, Google, "external-subject-owner", ct);

        var userEmail = UniqueEmail();
        var user = new User(userEmail, "hash", UserRole.User, DateTimeOffset.UtcNow);
        await AddUserAsync(user, ct);

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            WithServiceAsync(service =>
                service.LinkAsync(user.Id, Google, "external-subject-owner", userEmail, null, true, ct)));

        Assert.Equal(ServiceErrorKind.Conflict, exception.Kind);
        Assert.Null(await FindSubjectAsync(user.Id, Google, ct));
        Assert.Equal("external-subject-owner", await FindSubjectAsync(owner.Id, Google, ct));
    }

    [Fact]
    public async Task Explicit_link_rejects_second_identity_for_same_account()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        var user = await AddLinkedUserAsync(email, Google, "external-subject-current", ct);

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            WithServiceAsync(service =>
                service.LinkAsync(user.Id, Google, "external-subject-other", email, null, true, ct)));

        Assert.Equal(ServiceErrorKind.Conflict, exception.Kind);
        Assert.Equal("external-subject-current", await FindSubjectAsync(user.Id, Google, ct));
    }

    [Fact]
    public async Task Explicit_link_is_idempotent_for_same_identity()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        var user = await AddLinkedUserAsync(email, Google, "external-subject-same", ct);

        var authenticated = await WithServiceAsync(service =>
            service.LinkAsync(user.Id, Google, "external-subject-same", email, null, true, ct));

        Assert.Equal(user.Id, authenticated.Id);
        Assert.Equal("external-subject-same", await FindSubjectAsync(user.Id, Google, ct));
        Assert.Equal(1, await CountProvidersAsync(user.Id, Google, ct));
    }

    [Fact]
    public async Task Second_provider_links_to_the_same_account()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        var user = await AddLinkedUserAsync(email, Google, "external-subject-google", ct);

        await WithServiceAsync(service =>
            service.LinkAsync(user.Id, Yandex, "external-subject-yandex", UserEmail("yandex"), null, true, ct));

        var authenticated = await WithServiceAsync(service =>
            service.SignInAsync(Google, "external-subject-google", email, null, true, ct));

        Assert.Equal("external-subject-google", await FindSubjectAsync(user.Id, Google, ct));
        Assert.Equal("external-subject-yandex", await FindSubjectAsync(user.Id, Yandex, ct));
        Assert.Contains(Google, authenticated.LinkedProviders);
        Assert.Contains(Yandex, authenticated.LinkedProviders);
    }

    [Fact]
    public async Task New_account_saves_provider_name_as_display_name()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();

        await WithServiceAsync(
            service => service.SignInAsync(Google, "external-subject-name-new", email, "Иван Иванов", true, ct));

        Assert.Equal("Иван Иванов", (await FindUserAsync(email, ct)).DisplayName);
    }

    [Fact]
    public async Task Auto_link_fills_empty_display_name()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        var user = new User(email, "hash", UserRole.User, DateTimeOffset.UtcNow);
        await AddUserAsync(user, ct);

        await WithServiceAsync(
            service => service.SignInAsync(Google, "external-subject-name-auto", email, "Пётр", true, ct));

        Assert.Equal("Пётр", (await FindUserAsync(email, ct)).DisplayName);
    }

    [Fact]
    public async Task Explicit_link_fills_empty_display_name()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        var user = new User(email, "hash", UserRole.User, DateTimeOffset.UtcNow);
        await AddUserAsync(user, ct);

        await WithServiceAsync(
            service => service.LinkAsync(user.Id, Google, "external-subject-name-link", email, "Мария", true, ct));

        Assert.Equal("Мария", (await FindUserAsync(email, ct)).DisplayName);
    }

    [Fact]
    public async Task Repeated_sign_in_does_not_overwrite_existing_display_name()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        const string subject = "external-subject-name-keep";

        await WithServiceAsync(service => service.SignInAsync(Google, subject, email, "Первый", true, ct));
        await WithServiceAsync(service => service.SignInAsync(Google, subject, email, "Другой", true, ct));

        Assert.Equal("Первый", (await FindUserAsync(email, ct)).DisplayName);
    }

    [Fact]
    public async Task Missing_provider_name_does_not_break_sign_in()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();

        var authenticated = await WithServiceAsync(
            service => service.SignInAsync(Yandex, "external-subject-name-absent", email, null, true, ct));

        var stored = await FindUserAsync(email, ct);
        Assert.Equal(stored.Id, authenticated.Id);
        Assert.Null(stored.DisplayName);
    }

    private static string UniqueEmail() => $"{EmailPrefix}{Guid.NewGuid():N}@example.com";

    private static string UserEmail(string label) => $"{EmailPrefix}{label}-{Guid.NewGuid():N}@example.com";

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private async Task<T> WithServiceAsync<T>(Func<ExternalAuthService, Task<T>> action)
        => await TestDatabase.WithServicesAsync(
            _factory,
            provider => action(provider.GetRequiredService<ExternalAuthService>()));

    private async Task<User> FindUserAsync(string email, CancellationToken cancellationToken)
        => await TestDatabase.WithContextAsync(_factory, async db =>
            await db.Users
                .AsNoTracking()
                .FirstAsync(candidate => candidate.Email == email, cancellationToken));

    private async Task<int> CountUsersAsync(string email, CancellationToken cancellationToken)
        => await TestDatabase.WithContextAsync(_factory, async db =>
            await db.Users.CountAsync(candidate => candidate.Email == email, cancellationToken));

    private async Task<string?> FindSubjectAsync(Guid userId, string provider, CancellationToken cancellationToken)
        => await TestDatabase.WithContextAsync(_factory, async db =>
            await db.ExternalLogins
                .AsNoTracking()
                .Where(login => login.UserId == userId && login.Provider == provider)
                .Select(login => login.Subject)
                .FirstOrDefaultAsync(cancellationToken));

    private async Task<int> CountProvidersAsync(Guid userId, string provider, CancellationToken cancellationToken)
        => await TestDatabase.WithContextAsync(_factory, async db =>
            await db.ExternalLogins.CountAsync(
                login => login.UserId == userId && login.Provider == provider,
                cancellationToken));

    private async Task AddUserAsync(User user, CancellationToken cancellationToken)
        => await TestDatabase.WithContextAsync(_factory, async db =>
        {
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        });

    private async Task<User> AddLinkedUserAsync(
        string email,
        string provider,
        string subject,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.CreatePasswordless(email, "placeholder-hash", now);

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            db.Users.Add(user);
            db.ExternalLogins.Add(new ExternalLogin(user.Id, provider, subject, now));
            await db.SaveChangesAsync(cancellationToken);
        });

        return user;
    }
}
