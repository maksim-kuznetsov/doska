using Doska.Domain.Enums;

namespace Doska.Domain.Entities;

/// <summary>
/// Одноразовый токен для письма: подтверждение почты или сброс пароля.
/// В базе хранится только хеш токена (SHA-256), сам токен уходит в письме.
/// </summary>
public class UserToken : Entity
{
    /// <summary>Владелец токена.</summary>
    public Guid UserId { get; set; }

    /// <summary>Владелец токена.</summary>
    public User? User { get; set; }

    /// <summary>Хеш токена (SHA-256); уникален.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Назначение токена.</summary>
    public TokenPurpose Purpose { get; set; }

    /// <summary>Момент истечения срока действия токена.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Момент использования токена; <c>null</c> — токен ещё не использован.</summary>
    public DateTimeOffset? UsedAt { get; set; }
}
