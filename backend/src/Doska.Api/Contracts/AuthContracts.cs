using Doska.Application.Auth;

namespace Doska.Api.Contracts;

/// <summary>Запрос регистрации.</summary>
public sealed record RegisterRequest(string Email, string Password);

/// <summary>Запрос входа.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Запрос подтверждения email.</summary>
public sealed record ConfirmEmailRequest(string Token);

/// <summary>Ответ с antiforgery-токеном.</summary>
public sealed record CsrfResponse(string Token);

/// <summary>Сведения о текущем пользователе.</summary>
public sealed record UserResponse(Guid Id, string Email, string Role, bool IsTrusted, bool IsEmailConfirmed)
{
    /// <summary>Создаёт ответ из сведений о пользователе.</summary>
    public static UserResponse From(AuthenticatedUser user)
        => new(user.Id, user.Email, user.Role, user.IsTrusted, user.IsEmailConfirmed);
}
