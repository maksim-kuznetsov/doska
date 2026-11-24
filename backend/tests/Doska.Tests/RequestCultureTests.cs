using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Doska.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Doska.Tests;

/// <summary>
/// Проверяет, что разбор числовых полей запроса не зависит от локали сервера:
/// значение с десятичной точкой должно разбираться и на хосте с локалью ru-RU.
/// </summary>
public sealed class RequestCultureTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RequestCultureTests(ApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Культура разбора числовых полей зафиксирована инвариантной.</summary>
    [Fact]
    public void Number_binding_culture_is_invariant()
    {
        var options = _factory.Services
            .GetRequiredService<IOptions<RequestLocalizationOptions>>()
            .Value;

        Assert.Equal(CultureInfo.InvariantCulture, options.DefaultRequestCulture.Culture);
        Assert.Equal(CultureInfo.InvariantCulture, Assert.Single(options.SupportedCultures!));
        Assert.Equal(CultureInfo.InvariantCulture, Assert.Single(options.SupportedUICultures!));
    }

    /// <summary>
    /// Фильтры поиска принимают десятичную точку независимо от заголовка Accept-Language.
    /// </summary>
    [Fact]
    public async Task Search_filters_accept_decimal_point()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU");

        var response = await client.GetAsync(
            "/api/ads?minPrice=100.5&maxPrice=9999999999.99",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
