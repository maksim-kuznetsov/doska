using Doska.Domain.Enums;

namespace Doska.Domain.Entities;

/// <summary>Учётная запись пользователя.</summary>
public class User : Entity
{
    /// <summary>Адрес электронной почты; уникален, используется для входа.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Хеш пароля; исходный пароль не хранится.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Роль пользователя; у пользователя ровно одна роль.</summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>Признак подтверждения адреса электронной почты.</summary>
    public bool IsEmailConfirmed { get; set; }

    /// <summary>Признак доверенного автора: его объявления публикуются без проверки.</summary>
    public bool IsTrusted { get; set; }

    /// <summary>Счётчик одобренных модератором объявлений; при достижении порога автор становится доверенным.</summary>
    public int ApprovedAdsCount { get; set; }

    /// <summary>Момент блокировки; <c>null</c> — пользователь не заблокирован.</summary>
    public DateTimeOffset? BlockedAt { get; set; }

    /// <summary>Объявления пользователя.</summary>
    public ICollection<Ad> Ads { get; set; } = new List<Ad>();

    /// <summary>Жалобы, поданные пользователем.</summary>
    public ICollection<AdComplaint> Complaints { get; set; } = new List<AdComplaint>();

    /// <summary>Одноразовые токены пользователя.</summary>
    public ICollection<UserToken> Tokens { get; set; } = new List<UserToken>();
}
