namespace Doska.Domain.Enums;

/// <summary>Назначение одноразового токена, отправленного на почту.</summary>
public enum TokenPurpose
{
    /// <summary>Подтверждение адреса электронной почты при регистрации.</summary>
    EmailConfirmation = 0,

    /// <summary>Сброс пароля.</summary>
    PasswordReset = 1,
}
