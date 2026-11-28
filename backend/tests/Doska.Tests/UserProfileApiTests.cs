using System.Net;
using System.Net.Http.Json;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Doska.Tests;

/// <summary>Профиль пользователя: сохранение имени и контактов, смена пароля.</summary>
public sealed class UserProfileApiTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string Password = "secret123";
    private const string NewPassword = "newsecret123";

    private readonly ApiFactory _factory;

    public UserProfileApiTests(ApiFactory factory)
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
            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith("profile-")));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Profile_is_saved_and_returned_by_me()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        await RegisterAsync(client, ct);

        var saved = await client.PutJsonAsync(
            "/api/account/profile",
            new { displayName = "  Иван  ", contacts = "  +7 900 000-00-00  " },
            ct);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var payload = await saved.Content.ReadFromJsonAsync<ProfilePayload>(ct);
        Assert.Equal("Иван", payload!.DisplayName);
        Assert.Equal("+7 900 000-00-00", payload.Contacts);

        var current = await client.GetFromJsonAsync<ProfilePayload>("/api/auth/me", ct);
        Assert.Equal("Иван", current!.DisplayName);
        Assert.Equal("+7 900 000-00-00", current.Contacts);
    }

    [Fact]
    public async Task Blank_profile_values_are_stored_as_empty()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        await RegisterAsync(client, ct);

        var saved = await client.PutJsonAsync(
            "/api/account/profile",
            new { displayName = "   ", contacts = (string?)null },
            ct);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var current = await client.GetFromJsonAsync<ProfilePayload>("/api/auth/me", ct);
        Assert.Null(current!.DisplayName);
        Assert.Null(current.Contacts);
    }

    [Fact]
    public async Task Contacts_longer_than_limit_are_rejected()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        await RegisterAsync(client, ct);

        var response = await client.PutJsonAsync(
            "/api/account/profile",
            new { displayName = "Иван", contacts = new string('а', 501) },
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Display_name_longer_than_limit_is_rejected()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        await RegisterAsync(client, ct);

        var response = await client.PutJsonAsync(
            "/api/account/profile",
            new { displayName = new string('а', 101), contacts = "+7 900 000-00-00" },
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Profile_endpoints_require_authentication()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();

        var update = await client.PutJsonAsync(
            "/api/account/profile",
            new { displayName = "Иван", contacts = "+7 900 000-00-00" },
            ct);
        Assert.Equal(HttpStatusCode.Unauthorized, update.StatusCode);

        var password = await client.PostJsonAsync(
            "/api/account/password",
            new { currentPassword = Password, newPassword = NewPassword },
            ct);
        Assert.Equal(HttpStatusCode.Unauthorized, password.StatusCode);
    }

    [Fact]
    public async Task Change_password_updates_credentials()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var email = await RegisterAsync(client, ct);

        var changed = await client.PostJsonAsync(
            "/api/account/password",
            new { currentPassword = Password, newPassword = NewPassword },
            ct);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

        using var newClient = _factory.CreateClient();
        var newLogin = await newClient.PostJsonAsync(
            "/api/auth/login",
            new { email, password = NewPassword },
            ct);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);

        using var oldClient = _factory.CreateClient();
        var oldLogin = await oldClient.PostJsonAsync(
            "/api/auth/login",
            new { email, password = Password },
            ct);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
    }

    [Fact]
    public async Task Google_only_account_sets_first_password_without_current()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        var email = await RegisterAsync(client, ct);

        // Аккаунт после регистрации имитирует созданный через Google: сессия уже открыта,
        // но известного пароля у аккаунта нет.
        await TestDatabase.WithContextAsync(_factory, async db =>
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE users SET has_password = false WHERE email = {0}",
                [email],
                ct));

        var changed = await client.PostJsonAsync(
            "/api/account/password",
            new { currentPassword = (string?)null, newPassword = NewPassword },
            ct);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

        var current = await client.GetFromJsonAsync<ProfilePayload>("/api/auth/me", ct);
        Assert.True(current!.HasPassword);

        using var newClient = _factory.CreateClient();
        var newLogin = await newClient.PostJsonAsync(
            "/api/auth/login",
            new { email, password = NewPassword },
            ct);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task Change_password_rejects_wrong_current_password()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        await RegisterAsync(client, ct);

        var response = await client.PostJsonAsync(
            "/api/account/password",
            new { currentPassword = "wrong-password", newPassword = NewPassword },
            ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Change_password_rejects_weak_new_password()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var client = _factory.CreateClient();
        await RegisterAsync(client, ct);

        var response = await client.PostJsonAsync(
            "/api/account/password",
            new { currentPassword = Password, newPassword = "123" },
            ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        var email = $"profile-{Guid.NewGuid():N}@example.com";
        var response = await client.PostJsonAsync(
            "/api/auth/register",
            new { email, password = Password },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return email;
    }

    private sealed record ProfilePayload(
        Guid Id,
        string Email,
        string Role,
        bool IsTrusted,
        bool IsEmailConfirmed,
        string? DisplayName,
        string? Contacts,
        bool HasPassword,
        IReadOnlyList<string> LinkedProviders);
}
