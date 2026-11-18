using Doska.Domain.Enums;

namespace Doska.Domain.Entities;

/// <summary>Жалоба на объявление. Пользователь может пожаловаться на объявление один раз.</summary>
public class AdComplaint : Entity
{
    /// <summary>Объявление, на которое подана жалоба.</summary>
    public Guid AdId { get; set; }

    /// <summary>Объявление, на которое подана жалоба.</summary>
    public Ad? Ad { get; set; }

    /// <summary>Автор жалобы.</summary>
    public Guid AuthorId { get; set; }

    /// <summary>Автор жалобы.</summary>
    public User? Author { get; set; }

    /// <summary>Текст жалобы.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Состояние жалобы.</summary>
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;

    /// <summary>Момент закрытия жалобы; <c>null</c> — жалоба открыта.</summary>
    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>Модератор, закрывший жалобу.</summary>
    public Guid? ClosedByUserId { get; set; }

    /// <summary>Модератор, закрывший жалобу.</summary>
    public User? ClosedByUser { get; set; }
}
