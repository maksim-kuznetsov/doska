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

            foreach (var resetToken in stored)
            {
                resetToken.ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1);
            }

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
        var marker = "token=";
        var index = message.Body.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(index >= 0, "В письме нет ссылки сброса.");

        return message.Body[(index + marker.Length)..].Trim();
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

            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = passwordHasher.Hash(StaffPassword),
                Role = role,
                IsEmailConfirmed = true,
            });

            await db.SaveChangesAsync(cancellationToken);
            return 0;
        });

        return email;
    }

    private sealed record AdminUserPayload(
        Guid Id,
        string Email,
        string Role,
        bool IsTrusted,
        bool IsBlocked,
        bool IsEmailConfirmed);
}