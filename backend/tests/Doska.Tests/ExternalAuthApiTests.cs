using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Doska.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Doska.Tests;

/// <summary>Публичный список провайдеров и старт входа при выключенных настройках.</summary>
public sealed class ProviderStatusTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ProviderStatusTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Providers_reports_known_providers_disabled_without_credentials()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        var providers = await client.GetFromJsonAsync<ProvidersPayload>("/api/auth/providers", ct);

        Assert.NotNull(providers);
        Assert.Contains(providers!.Providers, provider => provider.Id == "google" && !provider.Enabled);
        Assert.Contains(providers.Providers, provider => provider.Id == "yandex" && !provider.Enabled);
    }

    [Fact]
    public async Task Start_without_credentials_returns_404()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/google/start", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_provider_start_returns_404()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/unknown/start", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_link_returns_401()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/google/link", ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record ProvidersPayload(IReadOnlyList<ProviderInfo> Providers);
    private sealed record ProviderInfo(string Id, string Name, bool Enabled);
}

/// <summary>Старт входа при настроенных провайдерах.</summary>
public sealed class ConfiguredProviderApiTests : IClassFixture<ConfiguredProvidersApiFactory>, IAsyncLifetime
{
    private const string Password = "secret123";

    private readonly ConfiguredProvidersApiFactory _factory;

    public ConfiguredProviderApiTests(ConfiguredProvidersApiFactory factory)
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
            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith("provider-api-")));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Providers_reports_google_and_yandex_enabled()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientWithoutRedirects();

        var providers = await client.GetFromJsonAsync<ProvidersPayload>("/api/auth/providers", ct);

        Assert.NotNull(providers);
        Assert.Contains(providers!.Providers, provider => provider.Id == "google" && provider.Enabled);
        Assert.Contains(providers.Providers, provider => provider.Id == "yandex" && provider.Enabled);
    }

    [Fact]
    public async Task Start_redirects_to_google()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientWithoutRedirects();

        var response = await client.GetAsync("/api/auth/google/start?redirect=/ads", ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("https://accounts.google.com/", location, StringComparison.Ordinal);

        Assert.Equal(
            "https://doska.example.com:8443/api/auth/google/callback",
            RedirectUriOf(location));
    }

    [Fact]
    public async Task Start_redirects_to_yandex()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientWithoutRedirects();

        var response = await client.GetAsync("/api/auth/yandex/start?redirect=/ads", ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("https://oauth.yandex.ru/", location, StringComparison.Ordinal);

        Assert.Equal(
            "https://doska.example.com:8443/api/auth/yandex/callback",
            RedirectUriOf(location));
    }

    [Fact]
    public async Task Authenticated_link_redirects_to_google()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientWithoutRedirects();
        var email = $"provider-api-{Guid.NewGuid():N}@example.com";

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

    private static string RedirectUriOf(string location)
    {
        var match = Regex.Match(location, "redirect_uri=([^&]+)");
        Assert.True(match.Success);
        return Uri.UnescapeDataString(match.Groups[1].Value);
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

    private sealed record ProvidersPayload(IReadOnlyList<ProviderInfo> Providers);
    private sealed record ProviderInfo(string Id, string Name, bool Enabled);
}

/// <summary>Фабрика с настроенными кредами Google и Яндекса для проверки challenge.</summary>
public class ConfiguredProvidersApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Google:ClientId", "test-client");
        builder.UseSetting("Google:ClientSecret", "test-secret");
        builder.UseSetting("Yandex:ClientId", "test-yandex-client");
        builder.UseSetting("Yandex:ClientSecret", "test-yandex-secret");
        builder.UseSetting("App:PublicBaseUrl", "https://doska.example.com:8443");
    }
}

/// <summary>Фабрика с низким лимитом чувствительных запросов для проверки 429.</summary>
public sealed class LowProviderRateLimitApiFactory : ConfiguredProvidersApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimit:Sensitive:PermitLimit", "1");
    }
}

/// <summary>Ограничение частоты запуска внешнего входа.</summary>
public sealed class ProviderRateLimitApiTests : IClassFixture<LowProviderRateLimitApiFactory>
{
    private readonly LowProviderRateLimitApiFactory _factory;

    public ProviderRateLimitApiTests(LowProviderRateLimitApiFactory factory)
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
