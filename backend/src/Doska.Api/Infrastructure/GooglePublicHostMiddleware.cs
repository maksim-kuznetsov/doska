using Doska.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Api.Infrastructure;

/// <summary>
/// Подставляет публичный адрес сайта в Host для Google OAuth. Обратный прокси
/// может не передать порт, а redirect_uri в Google должен совпадать точно.
/// </summary>
public static class GooglePublicHostMiddleware
{
    /// <summary>Включает подстановку публичного Host для путей Google-входа.</summary>
    public static IApplicationBuilder UseGooglePublicHost(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(
                "/api/auth/google",
                StringComparison.OrdinalIgnoreCase))
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
}