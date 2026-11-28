using Doska.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Api.Infrastructure;

/// <summary>
/// Подставляет публичный адрес сайта в Host для внешнего входа. Обратный прокси
/// может не передать порт, а redirect_uri у провайдера должен совпадать точно.
/// </summary>
public static class ExternalAuthPublicHostMiddleware
{
    private const string Prefix = "/api/auth";

    /// <summary>Включает подстановку публичного Host для путей внешнего входа.</summary>
    public static IApplicationBuilder UseExternalAuthPublicHost(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            if (IsExternalAuthPath(context))
            {
                var publicUrl = context.RequestServices.GetRequiredService<IPublicUrlProvider>();

                if (Uri.TryCreate(publicUrl.BaseUrl, UriKind.Absolute, out var baseUri)
                    && (baseUri.Scheme == Uri.UriSchemeHttps || baseUri.Scheme == Uri.UriSchemeHttp))
                {
                    context.Request.Scheme = baseUri.Scheme;
                    context.Request.Host = new HostString(baseUri.Authority);
                }
            }

            await next();
        });

    /// <summary>Определяет, относится ли путь к маршрутам внешнего входа.</summary>
    private static bool IsExternalAuthPath(HttpContext context)
    {
        var path = context.Request.Path;

        if (!path.StartsWithSegments(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = path.Value![Prefix.Length..].TrimStart('/');
        var provider = remainder.Split('/', 2)[0];

        var registry = context.RequestServices.GetRequiredService<IExternalAuthProviderRegistry>();
        return registry.Find(provider) is not null;
    }
}
