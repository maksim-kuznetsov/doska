using Doska.Domain.Enums;

namespace Doska.Domain.Entities;

/// <summary>Объявление.</summary>
public class Ad : Entity
{
    /// <summary>Автор объявления.</summary>
    public Guid AuthorId { get; set; }

    /// <summary>Автор объявления.</summary>
    public User? Author { get; set; }

    /// <summary>Категория объявления: всегда подраздел.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Категория объявления.</summary>
    public Category? Category { get; set; }

    /// <summary>Город объявления.</summary>
    public Guid CityId { get; set; }

    /// <summary>Город объявления.</summary>
    public City? City { get; set; }

    /// <summary>Заголовок объявления.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Описание объявления.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Цена; неотрицательное значение.</summary>
    public decimal Price { get; set; }

    /// <summary>Контактные данные автора в свободной форме.</summary>
    public string Contacts { get; set; } = string.Empty;

    /// <summary>Текущее состояние объявления.</summary>
    public AdStatus Status { get; set; } = AdStatus.OnModeration;

    /// <summary>Причина отклонения или скрытия; заполняется модератором.</summary>
    public string? ModerationReason { get; set; }

    /// <summary>
    /// Начало текущего срока публикации. Отдельного поля окончания нет:
    /// объявление уходит в архив, когда прошло больше семи суток с этого момента.
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>Момент отправки напоминания об истечении срока; <c>null</c> — напоминание не отправлено.</summary>
    public DateTimeOffset? ReminderSentAt { get; set; }

    /// <summary>Фотографии объявления; не более пяти.</summary>
    public ICollection<AdPhoto> Photos { get; set; } = new List<AdPhoto>();

    /// <summary>Жалобы на объявление.</summary>
    public ICollection<AdComplaint> Complaints { get; set; } = new List<AdComplaint>();
}
