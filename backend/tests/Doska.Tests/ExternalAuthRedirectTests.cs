using Doska.Api.Infrastructure;

namespace Doska.Tests;

/// <summary>Безопасный разбор адреса возврата после внешнего входа.</summary>
public sealed class ExternalAuthRedirectTests
{
    [Theory]
    [InlineData("/", "/")]
    [InlineData("/ads", "/ads")]
    [InlineData("/profile?google=linked", "/profile?google=linked")]
    [InlineData("  /ads?page=2  ", "/ads?page=2")]
    public void Local_path_is_kept(string value, string expected)
    {
        Assert.Equal(expected, ExternalAuthRedirects.SafeLocalPath(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://evil.example.com")]
    [InlineData("//evil.example.com")]
    [InlineData("/\\evil.example.com")]
    [InlineData("/ads\\..\\evil")]
    [InlineData("/%2f%2fevil.example.com")]
    [InlineData("/%5cevil.example.com")]
    public void External_or_unsafe_path_falls_back_to_home(string? value)
    {
        Assert.Equal("/", ExternalAuthRedirects.SafeLocalPath(value));
    }

    [Fact]
    public void Absolute_url_is_built_from_public_base()
    {
        Assert.Equal(
            "https://doska.example.com/ads",
            ExternalAuthRedirects.ToAbsolute("https://doska.example.com/", "/ads"));
    }

    [Fact]
    public void Error_path_appends_provider_query_parameters_safely()
    {
        Assert.Equal(
            "/login?google=error&reason=blocked",
            ExternalAuthRedirects.ErrorPath("/login", "google", "blocked"));

        Assert.Equal(
            "/profile?from=google&yandex=error&reason=conflict",
            ExternalAuthRedirects.ErrorPath("/profile?from=google", "yandex", "conflict"));
    }
}
