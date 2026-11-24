using System.Security.Claims;
using Doska.Application.Auth;
using Doska.Application.Common;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Doska.Api.Infrastructure;

/// <summary>Извлечение данных текущего пользователя из claims и построение принципала сессии.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Возвращает идентификатор текущего пользователя.</summary>
    /// <exception cref="ServiceException">Идентификатор отсутствует или некорректен.</exception>
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(value, out var userId))
        {
            return userId;
        }

        throw new ServiceException(ServiceErrorKind.Unauthorized, "Требуется вход.");
    }

    /// <summary>Возвращает идентификатор текущего пользователя или <c>null</c> для анонимного запроса.</summary>
    public static Guid? GetUserIdOrNull(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    /// <summary>Создаёт принципал аутентифицированной сессии из сведений о пользователе.</summary>
    public static ClaimsPrincipal CreateSessionPrincipal(AuthenticatedUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        return new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
