using Doska.Application.Auth;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Tests;

/// <summary>Разрешение Google-идентичности и привязка к аккаунту.</summary>
public sealed class GoogleAuthServiceTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string EmailPrefix = "google-auth-";

    private readonly ApiFactory _factory;

    public GoogleAuthServiceTests(ApiFactory factory)
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
    public async Task Unknown_google_identity_creates_confirmed_passwordless_account()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        const string subject = "google-subject-new";

        var authenticated = await WithServiceAsync(
            service => service.SignInAsync(subject, email, emailVerified: true, ct));

        var user = await FindUserAsync(email, ct);
        Assert.Equal(user.Id, authenticated.Id);
        Assert.Equal(subject, user.GoogleSubject);
        Assert.True(user.IsEmailConfirmed);
        Assert.False(user.HasPassword);
        Assert.Equal(UserRole.User, user.Role);
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
            service => service.SignInAsync("google-subject-auto", email.ToUpperInvariant(), true, ct));

        var stored = await FindUserAsync(email, ct);
        Assert.Equal(user.Id, authenticated.Id);
        Assert.Equal("google-subject-auto", stored.GoogleSubject);
        Assert.True(stored.IsEmailConfirmed);
        Assert.True(stored.HasPassword);
        Assert.Equal(1, await CountUsersAsync(email, ct));
    }

    [Fact]
    public async Task Linked_subject_wins_over_changed_email()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        const string subject = "google-subject-stable";

        var first = await WithServiceAsync(
            service => service.SignInAsync(subject, email, true, ct));

        var second = await WithServiceAsync(
            service => service.SignInAsync(subject, UniqueEmail(), true, ct));

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await CountUsersAsync(email, ct));
    }

    [Fact]
    public async Task Blocked_linked_account_is_rejected()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        const string subject = "google-subject-blocked";

        await WithServiceAsync(service => service.SignInAsync(subject, email, true, ct));

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var user = await db.Users.FirstAsync(candidate => candidate.Email == email, ct);
            user.Block(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);
        });

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            WithServiceAsync(service => service.SignInAsync(subject, email, true, ct)));

        Assert.Equal(ServiceErrorKind.Forbidden, exception.Kind);
    }

    [Fact]
    public async Task Email_of_account_linked_to_another_subject_is_not_relinked()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        var user = User.CreateWithGoogle(
            email,
            "google-subject-first",
            "placeholder",
            DateTimeOffset.UtcNow);
        await AddUserAsync(user, ct);

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            WithServiceAsync(service => service.SignInAsync("google-subject-second", email, true, ct)));

        Assert.Equal(ServiceErrorKind.Conflict, exception.Kind);
        Assert.Equal("google-subject-first", (await FindUserAsync(email, ct)).GoogleSubject);
    }

    [Fact]
    public async Task Unverified_email_is_rejected_without_creating_account()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            WithServiceAsync(service => service.SignInAsync("google-subject-unverified", email, false, ct)));

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
            service => service.LinkAsync(user.Id, "google-subject-link", UniqueEmail(), true, ct));

        var stored = await FindUserAsync(accountEmail, ct);
        Assert.Equal(user.Id, authenticated.Id);
        Assert.Equal("google-subject-link", stored.GoogleSubject);
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
            service => service.LinkAsync(user.Id, "google-subject-link-match", email, true, ct));

        Assert.True((await FindUserAsync(email, ct)).IsEmailConfirmed);
    }

    [Fact]
    public async Task Explicit_link_rejects_identity_owned_by_another_account()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var ownerEmail = UniqueEmail();
        var owner = User.CreateWithGoogle(
            ownerEmail,
            "google-subject-owner",
            "placeholder",
            DateTimeOffset.UtcNow);
        await AddUserAsync(owner, ct);

        var userEmail = UniqueEmail();
        var user = new User(userEmail, "hash", UserRole.User, DateTimeOffset.UtcNow);
        await AddUserAsync(user, ct);

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            WithServiceAsync(service =>
                service.LinkAsync(user.Id, "google-subject-owner", userEmail, true, ct)));

        Assert.Equal(ServiceErrorKind.Conflict, exception.Kind);
        Assert.Null((await FindUserAsync(userEmail, ct)).GoogleSubject);
    }

    [Fact]
    public async Task Explicit_link_rejects_second_identity_for_same_account()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        var user = User.CreateWithGoogle(
            email,
            "google-subject-current",
            "placeholder",
            DateTimeOffset.UtcNow);
        await AddUserAsync(user, ct);

        var exception = await Assert.ThrowsAsync<ServiceException>(() =>
            WithServiceAsync(service =>
                service.LinkAsync(user.Id, "google-subject-other", email, true, ct)));

        Assert.Equal(ServiceErrorKind.Conflict, exception.Kind);
        Assert.Equal("google-subject-current", (await FindUserAsync(email, ct)).GoogleSubject);
    }

    [Fact]
    public async Task Explicit_link_is_idempotent_for_same_identity()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = UniqueEmail();
        var user = User.CreateWithGoogle(
            email,
            "google-subject-same",
            "placeholder",
            DateTimeOffset.UtcNow);
        await AddUserAsync(user, ct);

        var authenticated = await WithServiceAsync(service =>
            service.LinkAsync(user.Id, "google-subject-same", email, true, ct));

        Assert.Equal(user.Id, authenticated.Id);
        Assert.Equal("google-subject-same", (await FindUserAsync(email, ct)).GoogleSubject);
    }

    private static string UniqueEmail() => $"{EmailPrefix}{Guid.NewGuid():N}@example.com";

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private async Task<T> WithServiceAsync<T>(Func<GoogleAuthService, Task<T>> action)
        => await TestDatabase.WithServicesAsync(
            _factory,
            provider => action(provider.GetRequiredService<GoogleAuthService>()));

    private async Task<User> FindUserAsync(string email, CancellationToken cancellationToken)
        => await TestDatabase.WithContextAsync(_factory, async db =>
            await db.Users
                .AsNoTracking()
                .FirstAsync(candidate => candidate.Email == email, cancellationToken));

    private async Task<int> CountUsersAsync(string email, CancellationToken cancellationToken)
        => await TestDatabase.WithContextAsync(_factory, async db =>
            await db.Users.CountAsync(candidate => candidate.Email == email, cancellationToken));

    private async Task AddUserAsync(User user, CancellationToken cancellationToken)
        => await TestDatabase.WithContextAsync(_factory, async db =>
        {
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
        });
}