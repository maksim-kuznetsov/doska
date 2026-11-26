using Doska.Domain.Enums;

namespace Doska.Domain.Entities;

/// <summary>Учётная запись пользователя.</summary>
public class User : Entity
{
    /// <summary>Число одобренных объявлений, после которого доверие выдаётся автоматически.</summary>
    public const int TrustThreshold = 3;

    /// <summary>Максимальная длина отображаемого имени.</summary>
    public const int MaxDisplayNameLength = 100;

    /// <summary>Адрес электронной почты; уникален, используется для входа.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Хеш пароля; исходный пароль не хранится.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Признак заданного пароля; у аккаунта, созданного через Google, пароля нет.</summary>
    public bool HasPassword { get; private set; } = true;

    /// <summary>Идентификатор Google-пользователя (claim <c>sub</c>); <c>null</c> — Google не привязан.</summary>
    public string? GoogleSubject { get; private set; }

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

    /// <summary>Отображаемое имя пользователя; <c>null</c> — не задано.</summary>
    public string? DisplayName { get; private set; }

    /// <summary>Контактные данные профиля в свободной форме; <c>null</c> — не заданы.</summary>
    public string? Contacts { get; private set; }

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
    /// Создаёт подтверждённый аккаунт, привязанный к Google, без известного пароля.
    /// Хеш-заглушка нужна только для совместимости схемы и не позволяет войти по паролю.
    /// </summary>
    /// <param name="email">Подтверждённый адрес электронной почты из Google.</param>
    /// <param name="googleSubject">Идентификатор Google-пользователя.</param>
    /// <param name="placeholderPasswordHash">Непригодный для входа хеш случайного значения.</param>
    /// <param name="now">Момент создания.</param>
    public static User CreateWithGoogle(
        string email,
        string googleSubject,
        string placeholderPasswordHash,
        DateTimeOffset now)
    {
        var user = new User(email, placeholderPasswordHash, UserRole.User, now)
        {
            GoogleSubject = NormalizeGoogleSubject(googleSubject),
            HasPassword = false,
            IsEmailConfirmed = true,
        };

        return user;
    }

    /// <summary>
    /// Привязывает Google-идентичность к аккаунту. Повторная привязка той же
    /// идентичности ничего не меняет; другая идентичность отклоняется.
    /// </summary>
    /// <param name="googleSubject">Идентификатор Google-пользователя.</param>
    /// <param name="now">Момент привязки.</param>
    public void LinkGoogle(string googleSubject, DateTimeOffset now)
    {
        var normalized = NormalizeGoogleSubject(googleSubject);

        if (string.Equals(GoogleSubject, normalized, StringComparison.Ordinal))
        {
            return;
        }

        if (GoogleSubject is not null)
        {
            throw new InvalidOperationException("К аккаунту уже привязана другая Google-идентичность.");
        }

        GoogleSubject = normalized;
        MarkUpdated(now);
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
        HasPassword = true;
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

    /// <summary>
    /// Обновляет профиль пользователя: отображаемое имя и контакты. Пустые значения
    /// (после обрезки пробелов) сохраняются как <c>null</c>.
    /// </summary>
    /// <param name="displayName">Новое отображаемое имя.</param>
    /// <param name="contacts">Новые контакты.</param>
    /// <param name="now">Момент изменения.</param>
    public void UpdateProfile(string? displayName, string? contacts, DateTimeOffset now)
    {
        DisplayName = Normalize(displayName);
        Contacts = Normalize(contacts);
        MarkUpdated(now);
    }

    /// <summary>
    /// Заполняет отображаемое имя из внешней идентичности Google, только если оно ещё не задано.
    /// Пустое или слишком длинное значение игнорируется, заданное имя не перезаписывается.
    /// </summary>
    /// <param name="displayName">Имя из Google.</param>
    /// <param name="now">Момент изменения.</param>
    public void ApplyGoogleDisplayName(string? displayName, DateTimeOffset now)
    {
        if (DisplayName is not null)
        {
            return;
        }

        var normalized = Normalize(displayName);

        if (normalized is null || normalized.Length > MaxDisplayNameLength)
        {
            return;
        }

        DisplayName = normalized;
        MarkUpdated(now);
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string NormalizeGoogleSubject(string googleSubject)
    {
        var trimmed = googleSubject?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ArgumentException("Идентификатор Google не может быть пустым.", nameof(googleSubject));
        }

        return trimmed;
    }
}
