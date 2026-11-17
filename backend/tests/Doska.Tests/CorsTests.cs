using System.Net;

namespace Doska.Tests;

public sealed class CorsTests : IClassFixture<ApiFactory>
{
    private const string AllowedOrigin = "http://localhost:5173";
    private const string ForeignOrigin = "http://example.com";

    private readonly ApiFactory _factory;

    public CorsTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Allowed_origin_receives_cors_headers()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", AllowedOrigin);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal(AllowedOrigin, values!.Single());
    }

    [Fact]
    public async Task Foreign_origin_receives_no_cors_headers()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", ForeignOrigin);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
