using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Doska.Application.Abstractions;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Tests;

/// <summary>Сброс пароля по одноразовой ссылке и управление ролями администратором.</summary>
public sealed class UserAccountApiTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string Password = "secret123";
    private const string NewPassword = "newsecret123";
    private const string StaffPassword = "staffpass123";

    private readonly ApiFactory _factory;

    public UserAccountApiTests(ApiFactory factory)
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
            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith("accounts-")));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Emails_contain_absolute_links()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var email = await RegisterAsync(client, ct);

        var confirmation = Assert.Single(
            _factory.Emails.Messages,
            message => message.Email == email && message.Subject == "Подтверждение email");
        Assert.Contains("https://doska.example.com/confirm-email?token=", confirmation.Body);

        _factory.Emails.Clear();
        var reset = await client.PostJsonAsync("/api/auth/forgot-password", new { email }, ct);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var resetMessage = Assert.Single(
            _factory.Emails.Messages,
            message => message.Email == email && message.Subject == "Сброс пароля");
        Assert.Contains("https://doska.example.com/reset-password?token=", resetMessage.Body);
    }

    [Fact]
    public async Task Confirmation_email_contains_url_safe_token()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var email = await RegisterAsync(client, ct);
        var token = GetConfirmationToken(email);

        Assert.Matches(new Regex("^[A-Za-z0-9_-]{43}$"), token);
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
    }

    [Fact]
    public async Task Confirmation_link_confirms_account_and_cannot_be_reused()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var email = await RegisterAsync(client, ct);
        var token = GetConfirmationToken(email);

        var confirm = await client.PostJsonAsync("/api/auth/confirm-email", new { token }, ct);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var current = await client.GetFromJsonAsync<CurrentUserPayload>("/api/auth/me", ct);
        Assert.True(current!.IsEmailConfirmed);

        var reused = await client.PostJsonAsync("/api/auth/confirm-email", new { token }, ct);
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
    }

    [Fact]
    public async Task Password_login_is_rejected_for_google_only_account()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var email = $"accounts-google-{Guid.NewGuid():N}@example.com";

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            db.Users.Add(User.CreateWithGoogle(
                email,
                $"google-subject-{Guid.NewGuid():N}",
                "placeholder-hash",
                DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(ct);
        });

        using var client = _factory.CreateClient();
        var response = await client.PostJsonAsync(
            "/api/auth/login",
            new { email, password = Password },
            ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Legacy_base64_confirmation_token_still_confirms_account()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var email = await RegisterAsync(client, ct);

        // Токен прежнего формата: стандартный Base64 из 32 байт, с завершающим «=».
        var legacyToken = Convert.ToBase64String(
            Enumerable.Range(0, 32).Select(value => (byte)value).ToArray());
        Assert.EndsWith("=", legacyToken);

        var legacyHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(legacyToken))).ToLowerInvariant();

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var user = await db.Users.FirstAsync(candidate => candidate.Email == email, ct);
            db.UserTokens.Add(new UserToken(
                user.Id,
                legacyHash,
                TokenPurpose.EmailConfirmation,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(ct);
        });

        var confirm = await client.PostJsonAsync(
            "/api/auth/confirm-email",
            new { token = legacyToken },
            ct);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var current = await client.GetFromJsonAsync<CurrentUserPayload>("/api/auth/me", ct);
        Assert.True(current!.IsEmailConfirmed);
    }

    [Fact]
    public async Task Resend_confirmation_sends_new_email_and_invalidates_previous_link()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var email = await RegisterAsync(client, ct);
        var firstToken = GetConfirmationToken(email);

        _factory.Emails.Clear();
        var resend = await client.PostJsonAsync("/api/auth/resend-confirmation", new { email }, ct);
        Assert.Equal(HttpStatusCode.NoContent, resend.StatusCode);

        var message = Assert.Single(
            _factory.Emails.Messages,
            candidate => candidate.Email == email && candidate.Subject == "Подтверждение email");
        var secondToken = ExtractToken(message.Body, "В письме нет ссылки подтверждения.");

        Assert.NotEqual(firstToken, secondToken);
        Assert.Matches(new Regex("^[A-Za-z0-9_-]{43}$"), secondToken);

        var stale = await client.PostJsonAsync(
            "/api/auth/confirm-email",
            new { token = firstToken },
            ct);
        Assert.Equal(HttpStatusCode.BadRequest, stale.StatusCode);

        var fresh = await client.PostJsonAsync(
            "/api/auth/confirm-email",
            new { token = secondToken },
            ct);
        Assert.Equal(HttpStatusCode.NoContent, fresh.StatusCode);
    }

    [Fact]
    public async Task Resend_confirmation_is_silent_for_unknown_and_confirmed_email()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var missingEmail = $"accounts-missing-{Guid.NewGuid():N}@example.com";

        _factory.Emails.Clear();
        var missing = await client.PostJsonAsync(
            "/api/auth/resend-confirmation",
            new { email = missingEmail },
            ct);
        Assert.Equal(HttpStatusCode.NoContent, missing.StatusCode);
        Assert.DoesNotContain(_factory.Emails.Messages, message => message.Email == missingEmail);

        var email = await RegisterAsync(client, ct);
        var token = GetConfirmationToken(email);
        var confirm = await client.PostJsonAsync("/api/auth/confirm-email", new { token }, ct);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        _factory.Emails.Clear();
        var confirmed = await client.PostJsonAsync("/api/auth/resend-confirmation", new { email }, ct);
        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);
        Assert.DoesNotContain(_factory.Emails.Messages, message => message.Email == email);
    }

    [Fact]
    public async Task Resend_confirmation_is_rate_limited()
    {
        await EnsureDatabaseAsync();

        using var factory = new LowLimitApiFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        var email = $"accounts-missing-{Guid.NewGuid():N}@example.com";

        var first = await client.PostJsonAsync("/api/auth/resend-confirmation", new { email }, ct);
        var second = await client.PostJsonAsync("/api/auth/resend-confirmation", new { email }, ct);
        var third = await client.PostJsonAsync("/api/auth/resend-confirmation", new { email }, ct);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.DoesNotContain(factory.Emails.Messages, message => message.Email == email);
    }

    [Fact]
    public async Task Reset_request_sends_link_only_for_existing_email()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var email = await RegisterAsync(client, ct);

        _factory.Emails.Clear();
        var existing = await client.PostJsonAsync("/api/auth/forgot-password", new { email }, ct);
        Assert.Equal(HttpStatusCode.NoContent, existing.StatusCode);
        Assert.Single(_factory.Emails.Messages, message => message.Email == email);

        var missingEmail = $"accounts-missing-{Guid.NewGuid():N}@example.com";
        var missing = await client.PostJsonAsync(
            "/api/auth/forgot-password",
            new { email = missingEmail },
            ct);

        Assert.Equal(HttpStatusCode.NoContent, missing.StatusCode);
        Assert.DoesNotContain(_factory.Emails.Messages, message => message.Email == missingEmail);
    }

    [Fact]
    public async Task Reset_link_changes_password_and_cannot_be_reused()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var email = await RegisterAsync(client, ct);
        var token = await RequestResetTokenAsync(client, email, ct);

        var reset = await client.PostJsonAsync(
            "/api/auth/reset-password",
            new { token, password = NewPassword },
            ct);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var newLogin = await client.PostJsonAsync(
            "/api/auth/login",
            new { email, password = NewPassword },
            ct);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);

        using var oldPasswordClient = _factory.CreateClient();
        var oldLogin = await oldPasswordClient.PostJsonAsync(
            "/api/auth/login",
            new { email, password = Password },
            ct);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var reused = await client.PostJsonAsync(
            "/api/auth/reset-password",
            new { token, password = "another123" },
            ct);
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
    }

    [Fact]
    public async Task Expired_reset_link_is_rejected()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var email = await RegisterAsync(client, ct);
        var token = await RequestResetTokenAsync(client, email, ct);

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            var user = await db.Users.FirstAsync(candidate => candidate.Email == email, ct);
            var stored = await db.UserTokens
                .Where(candidate => candidate.UserId == user.Id && candidate.Purpose == TokenPurpose.PasswordReset)
                .ToListAsync(ct);

            var now = DateTimeOffset.UtcNow;
            var expired = stored
                .Select(resetToken => new UserToken(
                    user.Id,
                    resetToken.TokenHash,
                    TokenPurpose.PasswordReset,
                    now.AddHours(-1),
                    now))
                .ToList();

            // Истёкший токен создаётся заново: отметку истечения задаёт конструктор.
            db.UserTokens.RemoveRange(stored);
            await db.SaveChangesAsync(ct);

            db.UserTokens.AddRange(expired);
            await db.SaveChangesAsync(ct);
        });

        var reset = await client.PostJsonAsync(
            "/api/auth/reset-password",
            new { token, password = NewPassword },
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
    }

    [Fact]
    public async Task New_reset_request_invalidates_the_previous_link()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var email = await RegisterAsync(client, ct);

        var firstToken = await RequestResetTokenAsync(client, email, ct);
        var secondToken = await RequestResetTokenAsync(client, email, ct);

        var firstAttempt = await client.PostJsonAsync(
            "/api/auth/reset-password",
            new { token = firstToken, password = NewPassword },
            ct);
        Assert.Equal(HttpStatusCode.BadRequest, firstAttempt.StatusCode);

        var secondAttempt = await client.PostJsonAsync(
            "/api/auth/reset-password",
            new { token = secondToken, password = NewPassword },
            ct);
        Assert.Equal(HttpStatusCode.NoContent, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task Admin_changes_role_and_moderator_is_forbidden()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin, ct), ct);

        var targetEmail = await RegisterAsync(_factory.CreateClient(), ct);

        var list = await admin.GetFromJsonAsync<List<AdminUserPayload>>("/api/admin/users", ct);
        var target = Assert.Single(list!, user => user.Email == targetEmail);
        Assert.Equal("User", target.Role);

        var change = await admin.PutJsonAsync(
            $"/api/admin/users/{target.Id}/role",
            new { role = "Moderator" },
            ct);
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        var updated = await change.Content.ReadFromJsonAsync<AdminUserPayload>(ct);
        Assert.Equal("Moderator", updated!.Role);

        var invalid = await admin.PutJsonAsync(
            $"/api/admin/users/{target.Id}/role",
            new { role = "Wizard" },
            ct);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using var moderator = _factory.CreateClient();
        await LoginAsync(moderator, await CreateStaffAsync(UserRole.Moderator, ct), ct);

        Assert.Equal(HttpStatusCode.Forbidden, (await moderator.GetAsync("/api/admin/users", ct)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await moderator.PutJsonAsync($"/api/admin/users/{target.Id}/role", new { role = "Admin" }, ct)).StatusCode);
    }

    [Fact]
    public async Task Role_change_applies_to_active_session_without_relogin()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin, ct), ct);

        using var user = _factory.CreateClient();
        var email = await RegisterAsync(user, ct);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await user.GetAsync("/api/admin/categories", ct)).StatusCode);

        var target = await FindUserAsync(admin, email, ct);

        var promote = await admin.PutJsonAsync(
            $"/api/admin/users/{target.Id}/role",
            new { role = "Admin" },
            ct);
        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await user.GetAsync("/api/admin/categories", ct)).StatusCode);

        var demote = await admin.PutJsonAsync(
            $"/api/admin/users/{target.Id}/role",
            new { role = "User" },
            ct);
        Assert.Equal(HttpStatusCode.OK, demote.StatusCode);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await user.GetAsync("/api/admin/categories", ct)).StatusCode);
    }

    [Fact]
    public async Task Blocked_user_loses_active_session()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin, ct), ct);

        using var user = _factory.CreateClient();
        var email = await RegisterAsync(user, ct);

        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/ads/mine", ct)).StatusCode);

        var target = await FindUserAsync(admin, email, ct);
        var blocked = await admin.PostJsonAsync($"/api/users/{target.Id}/block", null, ct);
        Assert.Equal(HttpStatusCode.OK, blocked.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await user.GetAsync("/api/ads/mine", ct)).StatusCode);
    }

    private async Task<AdminUserPayload> FindUserAsync(
        HttpClient admin,
        string email,
        CancellationToken cancellationToken)
    {
        var users = await admin.GetFromJsonAsync<List<AdminUserPayload>>("/api/admin/users", cancellationToken);
        return Assert.Single(users!, user => user.Email == email);
    }

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private async Task<string> RegisterAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var email = $"accounts-{Guid.NewGuid():N}@example.com";
        var response = await client.PostJsonAsync(
            "/api/auth/register",
            new { email, password = Password },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return email;
    }

    private string GetConfirmationToken(string email)
    {
        var message = _factory.Emails.Messages.Last(candidate =>
            candidate.Email == email && candidate.Subject == "Подтверждение email");

        return ExtractToken(message.Body, "В письме нет ссылки подтверждения.");
    }

    private async Task<string> RequestResetTokenAsync(
        HttpClient client,
        string email,
        CancellationToken cancellationToken)
    {
        _factory.Emails.Clear();

        var response = await client.PostJsonAsync(
            "/api/auth/forgot-password",
            new { email },
            cancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var message = Assert.Single(_factory.Emails.Messages, candidate => candidate.Email == email);
        return ExtractToken(message.Body, "В письме нет ссылки сброса.");
    }

    private static string ExtractToken(string body, string message)
    {
        var marker = "token=";
        var index = body.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(index >= 0, message);

        return body[(index + marker.Length)..].Trim();
    }

    private async Task LoginAsync(HttpClient client, string email, CancellationToken cancellationToken)
    {
        var response = await client.PostJsonAsync(
            "/api/auth/login",
            new { email, password = StaffPassword },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> CreateStaffAsync(UserRole role, CancellationToken cancellationToken)
    {
        var email = $"accounts-{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

        await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var passwordHasher = provider.GetRequiredService<IPasswordHasher>();

            db.Users.Add(TestData.CreateUser(
                email,
                role,
                passwordHash: passwordHasher.Hash(StaffPassword)));

            await db.SaveChangesAsync(cancellationToken);
            return 0;
        });

        return email;
    }

    private sealed record CurrentUserPayload(Guid Id, string Email, bool IsEmailConfirmed);

    private sealed record AdminUserPayload(
        Guid Id,
        string Email,
        string Role,
        bool IsTrusted,
        bool IsBlocked,
        bool IsEmailConfirmed);
}

/// <summary>Фабрика с низким лимитом повторных писем подтверждения для проверки ответа 429.</summary>
public sealed class LowLimitApiFactory : ApiFactory
{
    /// <inheritdoc />
    protected override int ConfirmationResendPermitLimit => 2;
}
