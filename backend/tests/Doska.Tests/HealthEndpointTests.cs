using System.Net;
using System.Net.Http.Json;

namespace Doska.Tests;

public sealed class HealthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HealthEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_health_returns_ok_with_environment_and_version()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<HealthPayload>(TestContext.Current.CancellationToken);

        Assert.NotNull(payload);
        Assert.Equal("Development", payload!.Environment);
        Assert.False(string.IsNullOrWhiteSpace(payload.Version));
    }

    private sealed record HealthPayload(string Environment, string Version);
}
