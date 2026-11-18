namespace Doska.Domain.Enums;

/// <summary>Состояние объявления в жизненном цикле.</summary>
public enum AdStatus
{
    /// <summary>На проверке модератором.</summary>
    OnModeration = 0,

    /// <summary>Активно и видно в поиске.</summary>
    Active = 1,

    /// <summary>Продано: автор отметил результат.</summary>
    Sold = 2,

    /// <summary>Архив: срок публикации истёк.</summary>
    Archived = 3,

    /// <summary>Отклонено модератором, причина в <c>moderation_reason</c>.</summary>
    Rejected = 4,

    /// <summary>Скрыто модератором, причина в <c>moderation_reason</c>.</summary>
    Hidden = 5,
}
