using System.Security.Claims;
using Doska.Application.Common;

namespace Doska.Api.Infrastructure;

/// <summary>Извлечение данных текущего пользователя из claims.</summary>
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
}
