using Doska.Infrastructure.Email;
using Microsoft.Extensions.Options;

namespace Doska.Tests;

/// <summary>Проверяет нормализацию публичного адреса сайта.</summary>
public sealed class PublicUrlProviderTests
{
    [Theory]
    [InlineData("https://doska.example.com", "https://doska.example.com")]
    [InlineData("https://doska.example.com/", "https://doska.example.com")]
    [InlineData("https://doska.example.com///", "https://doska.example.com")]
    public void Base_url_has_no_trailing_slash(string configured, string expected)
    {
        var provider = new ConfiguredPublicUrlProvider(
            Options.Create(new PublicUrlOptions { PublicBaseUrl = configured }));

        Assert.Equal(expected, provider.BaseUrl);
    }
}