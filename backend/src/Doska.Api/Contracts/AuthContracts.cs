using Doska.Application.Auth;

namespace Doska.Api.Contracts;

/// <summary>Запрос регистрации.</summary>
public sealed record RegisterRequest(string Email, string Password);

/// <summary>Запрос входа.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Запрос подтверждения email.</summary>
public sealed record ConfirmEmailRequest(string Token);

/// <summary>Запрос сброса пароля.</summary>
public sealed record ForgotPasswordRequest(string Email);

/// <summary>Запрос повторного письма подтверждения email.</summary>
public sealed record ResendConfirmationRequest(string Email);

/// <summary>Запрос установки нового пароля по одноразовой ссылке.</summary>
public sealed record ResetPasswordRequest(string Token, string Password);

/// <summary>Ответ с antiforgery-токеном.</summary>
public sealed record CsrfResponse(string Token);

/// <summary>Доступность внешних провайдеров входа.</summary>
public sealed record AuthProvidersResponse(bool Google);

/// <summary>Сведения о текущем пользователе.</summary>
public sealed record UserResponse(
    Guid Id,
    string Email,
    string Role,
    bool IsTrusted,
    bool IsEmailConfirmed,
    string? DisplayName,
    string? Contacts,
    bool HasPassword,
    bool IsGoogleLinked)
{
    /// <summary>Создаёт ответ из сведений о пользователе.</summary>
    public static UserResponse From(AuthenticatedUser user)
        => new(
            user.Id,
            user.Email,
            user.Role,
            user.IsTrusted,
            user.IsEmailConfirmed,
            user.DisplayName,
            user.Contacts,
            user.HasPassword,
            user.IsGoogleLinked);
}
