using Doska.Domain.Enums;

namespace Doska.Domain.Entities;

/// <summary>
/// Одноразовый токен для письма: подтверждение почты или сброс пароля.
/// В базе хранится только хеш токена (SHA-256), сам токен уходит в письме.
/// </summary>
public class UserToken : Entity
{
    /// <summary>Владелец токена.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Владелец токена.</summary>
    public User? User { get; private set; }

    /// <summary>Хеш токена (SHA-256); уникален.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Назначение токена.</summary>
    public TokenPurpose Purpose { get; private set; }

    /// <summary>Момент истечения срока действия токена.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Момент использования токена; <c>null</c> — токен ещё не использован.</summary>
    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>Создаёт пустой токен: используется EF Core при чтении из базы.</summary>
    private UserToken()
    {
    }

    /// <summary>Создаёт одноразовый токен.</summary>
    /// <param name="userId">Владелец токена.</param>
    /// <param name="tokenHash">Хеш токена.</param>
    /// <param name="purpose">Назначение токена.</param>
    /// <param name="expiresAt">Момент истечения срока действия.</param>
    /// <param name="now">Момент создания.</param>
    public UserToken(
        Guid userId,
        string tokenHash,
        TokenPurpose purpose,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
        : base(now)
    {
        UserId = userId;
        TokenHash = tokenHash;
        Purpose = purpose;
        ExpiresAt = expiresAt;
    }

    /// <summary>Отмечает токен использованным.</summary>
    /// <param name="now">Момент использования.</param>
    public void MarkUsed(DateTimeOffset now)
    {
        UsedAt = now;
        MarkUpdated(now);
    }
}
