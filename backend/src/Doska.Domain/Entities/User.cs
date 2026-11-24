using Doska.Domain.Enums;

namespace Doska.Domain.Entities;

/// <summary>Учётная запись пользователя.</summary>
public class User : Entity
{
    /// <summary>Число одобренных объявлений, после которого доверие выдаётся автоматически.</summary>
    public const int TrustThreshold = 3;

    /// <summary>Адрес электронной почты; уникален, используется для входа.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Хеш пароля; исходный пароль не хранится.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Роль пользователя; у пользователя ровно одна роль.</summary>
    public UserRole Role { get; private set; } = UserRole.User;

    /// <summary>Признак подтверждения адреса электронной почты.</summary>
    public bool IsEmailConfirmed { get; private set; }

    /// <summary>Признак доверенного автора: его объявления публикуются без проверки.</summary>
    public bool IsTrusted { get; private set; }

    /// <summary>Счётчик одобренных модератором объявлений; при достижении порога автор становится доверенным.</summary>
    public int ApprovedAdsCount { get; private set; }

    /// <summary>Момент блокировки; <c>null</c> — пользователь не заблокирован.</summary>
    public DateTimeOffset? BlockedAt { get; private set; }

    /// <summary>Объявления пользователя.</summary>
    public ICollection<Ad> Ads { get; } = new List<Ad>();

    /// <summary>Жалобы, поданные пользователем.</summary>
    public ICollection<AdComplaint> Complaints { get; } = new List<AdComplaint>();

    /// <summary>Одноразовые токены пользователя.</summary>
    public ICollection<UserToken> Tokens { get; } = new List<UserToken>();

    /// <summary>Признак блокировки пользователя.</summary>
    public bool IsBlocked => BlockedAt is not null;

    /// <summary>Создаёт пустого пользователя: используется EF Core при чтении из базы.</summary>
    private User()
    {
    }

    /// <summary>Создаёт учётную запись.</summary>
    /// <param name="email">Адрес электронной почты.</param>
    /// <param name="passwordHash">Хеш пароля.</param>
    /// <param name="role">Роль пользователя.</param>
    /// <param name="now">Момент создания.</param>
    public User(string email, string passwordHash, UserRole role, DateTimeOffset now)
        : base(now)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    /// <summary>
    /// Учитывает одобрение объявления автора: увеличивает счётчик и при достижении порога
    /// присваивает доверие. Ранее накопленный счётчик при отклонении не сбрасывается.
    /// </summary>
    /// <param name="now">Момент одобрения.</param>
    /// <returns><c>true</c>, если доверие было присвоено этим одобрением.</returns>
    public bool RegisterApprovedAd(DateTimeOffset now)
    {
        ApprovedAdsCount++;

        if (IsTrusted || ApprovedAdsCount < TrustThreshold)
        {
            MarkUpdated(now);
            return false;
        }

        IsTrusted = true;
        MarkUpdated(now);
        return true;
    }

    /// <summary>Вручную выдаёт доверие автору.</summary>
    /// <param name="now">Момент выдачи доверия.</param>
    public void GrantTrust(DateTimeOffset now)
    {
        IsTrusted = true;
        MarkUpdated(now);
    }

    /// <summary>
    /// Отзывает доверие у автора и обнуляет счётчик одобрений, чтобы доверие нельзя было
    /// вернуть одним следующим одобрением.
    /// </summary>
    /// <param name="now">Момент отзыва доверия.</param>
    public void RevokeTrust(DateTimeOffset now)
    {
        IsTrusted = false;
        ApprovedAdsCount = 0;
        MarkUpdated(now);
    }

    /// <summary>Меняет роль пользователя.</summary>
    /// <param name="role">Новая роль.</param>
    /// <param name="now">Момент изменения.</param>
    public void ChangeRole(UserRole role, DateTimeOffset now)
    {
        Role = role;
        MarkUpdated(now);
    }

    /// <summary>Задаёт новый хеш пароля.</summary>
    /// <param name="passwordHash">Новый хеш пароля.</param>
    /// <param name="now">Момент изменения.</param>
    public void SetPassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        MarkUpdated(now);
    }

    /// <summary>Подтверждает адрес электронной почты.</summary>
    /// <param name="now">Момент подтверждения.</param>
    public void ConfirmEmail(DateTimeOffset now)
    {
        IsEmailConfirmed = true;
        MarkUpdated(now);
    }

    /// <summary>Блокирует пользователя.</summary>
    /// <param name="now">Момент блокировки.</param>
    public void Block(DateTimeOffset now)
    {
        BlockedAt = now;
        MarkUpdated(now);
    }

    /// <summary>Снимает блокировку.</summary>
    /// <param name="now">Момент снятия блокировки.</param>
    public void Unblock(DateTimeOffset now)
    {
        BlockedAt = null;
        MarkUpdated(now);
    }
}
