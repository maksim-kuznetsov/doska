namespace Doska.Api.Infrastructure;

/// <summary>Имена политик авторизации.</summary>
public static class AuthorizationPolicies
{
    /// <summary>Модератор или администратор.</summary>
    public const string ModeratorOrAdmin = "ModeratorOrAdmin";

    /// <summary>Только администратор.</summary>
    public const string Admin = "Admin";
}

/// <summary>Имена политик ограничения частоты запросов.</summary>
public static class RateLimitPolicies
{
    /// <summary>Чувствительные операции: вход, регистрация, подача объявления.</summary>
    public const string Sensitive = "Sensitive";

    /// <summary>Повторная отправка письма подтверждения email.</summary>
    public const string ConfirmationResend = "ConfirmationResend";
}
