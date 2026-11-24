using Doska.Domain.Enums;

namespace Doska.Domain.Entities;

/// <summary>Жалоба на объявление. Пользователь может пожаловаться на объявление один раз.</summary>
public class AdComplaint : Entity
{
    /// <summary>Объявление, на которое подана жалоба.</summary>
    public Guid AdId { get; private set; }

    /// <summary>Объявление, на которое подана жалоба.</summary>
    public Ad? Ad { get; private set; }

    /// <summary>Автор жалобы.</summary>
    public Guid AuthorId { get; private set; }

    /// <summary>Автор жалобы.</summary>
    public User? Author { get; private set; }

    /// <summary>Текст жалобы.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Состояние жалобы.</summary>
    public ComplaintStatus Status { get; private set; } = ComplaintStatus.Open;

    /// <summary>Момент закрытия жалобы; <c>null</c> — жалоба открыта.</summary>
    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>Модератор, закрывший жалобу.</summary>
    public Guid? ClosedByUserId { get; private set; }

    /// <summary>Модератор, закрывший жалобу.</summary>
    public User? ClosedByUser { get; private set; }

    /// <summary>Создаёт пустую жалобу: используется EF Core при чтении из базы.</summary>
    private AdComplaint()
    {
    }

    /// <summary>Создаёт открытую жалобу на объявление.</summary>
    /// <param name="adId">Объявление, на которое подана жалоба.</param>
    /// <param name="authorId">Автор жалобы.</param>
    /// <param name="reason">Текст жалобы.</param>
    /// <param name="now">Момент подачи.</param>
    public AdComplaint(Guid adId, Guid authorId, string reason, DateTimeOffset now)
        : base(now)
    {
        AdId = adId;
        AuthorId = authorId;
        Reason = reason;
    }

    /// <summary>
    /// Закрывает жалобу решением модератора.
    /// </summary>
    /// <param name="moderatorId">Идентификатор модератора, принявшего решение.</param>
    /// <param name="now">Момент закрытия жалобы.</param>
    /// <exception cref="InvalidOperationException">Жалоба уже закрыта.</exception>
    public void Close(Guid moderatorId, DateTimeOffset now)
    {
        if (Status != ComplaintStatus.Open)
        {
            throw new InvalidOperationException("Жалоба уже закрыта.");
        }

        Status = ComplaintStatus.Closed;
        ClosedByUserId = moderatorId;
        ClosedAt = now;
        MarkUpdated(now);
    }
}
