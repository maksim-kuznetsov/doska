using System.Net;
using System.Net.Http.Json;
using Doska.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Doska.Tests;

/// <summary>Публичный статус Google-провайдера и старт входа при выключенной настройке.</summary>
public sealed class GoogleProviderStatusTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public GoogleProviderStatusTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Providers_reports_google_disabled_without_credentials()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        var providers = await client.GetFromJsonAsync<ProvidersPayload>("/api/auth/providers", ct);

        Assert.False(providers!.Google);
    }

    [Fact]
    public async Task Start_without_credentials_returns_503()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/google/start", ct);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_link_returns_401()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/google/link", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record ProvidersPayload(bool Google);
}

/// <summary>Старт Google-входа при настроенном провайдере.</summary>
public sealed class GoogleAuthApiTests : IClassFixture<GoogleConfiguredApiFactory>, IAsyncLifetime
{
    private const string Password = "secret123";

    private readonly GoogleConfiguredApiFactory _factory;

    public GoogleAuthApiTests(GoogleConfiguredApiFactory factory)
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
            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith("google-api-")));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Providers_reports_google_enabled()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientWithoutRedirects();

        var providers = await client.GetFromJsonAsync<ProvidersPayload>("/api/auth/providers", ct);

        Assert.True(providers!.Google);
    }

    [Fact]
    public async Task Start_redirects_to_google()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientWithoutRedirects();

        var response = await client.GetAsync("/api/auth/google/start?redirect=/ads", ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(
            "https://accounts.google.com/",
            response.Headers.Location!.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Authenticated_link_redirects_to_google()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientWithoutRedirects();
        var email = $"google-api-{Guid.NewGuid():N}@example.com";

        var registered = await client.PostJsonAsync(
            "/api/auth/register",
            new { email, password = Password },
            ct);
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);

        var response = await client.GetAsync("/api/auth/google/link", ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(
            "https://accounts.google.com/",
            response.Headers.Location!.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Anonymous_link_returns_401()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientWithoutRedirects();

        var response = await client.GetAsync("/api/auth/google/link", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient ClientWithoutRedirects()
        => _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private sealed record ProvidersPayload(bool Google);
}

/// <summary>Фабрика с настроенными кредами Google для проверки challenge.</summary>
public class GoogleConfiguredApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Google:ClientId", "test-client");
        builder.UseSetting("Google:ClientSecret", "test-secret");
    }
}

/// <summary>Фабрика с низким лимитом чувствительных запросов для проверки 429.</summary>
public sealed class LowGoogleRateLimitApiFactory : GoogleConfiguredApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimit:Sensitive:PermitLimit", "1");
    }
}

/// <summary>Ограничение частоты запуска Google-входа.</summary>
public sealed class GoogleRateLimitApiTests : IClassFixture<LowGoogleRateLimitApiFactory>
{
    private readonly LowGoogleRateLimitApiFactory _factory;

    public GoogleRateLimitApiTests(LowGoogleRateLimitApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Second_start_is_rate_limited()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var first = await client.GetAsync("/api/auth/google/start", ct);
        var second = await client.GetAsync("/api/auth/google/start", ct);

        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }
}