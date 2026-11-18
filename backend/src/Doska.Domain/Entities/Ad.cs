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

    /// <summary>
    /// Одобряет объявление, находящееся на проверке: переводит его в статус «активно»
    /// и фиксирует дату публикации.
    /// </summary>
    /// <param name="now">Момент принятия решения.</param>
    /// <exception cref="InvalidOperationException">Объявление не находится на проверке.</exception>
    public void Approve(DateTimeOffset now)
    {
        if (Status != AdStatus.OnModeration)
        {
            throw new InvalidOperationException("Одобрить можно только объявление на проверке.");
        }

        Status = AdStatus.Active;
        PublishedAt = now;
        ModerationReason = null;
    }

    /// <summary>
    /// Отклоняет объявление, находящееся на проверке, с обязательной причиной.
    /// </summary>
    /// <param name="reason">Причина отклонения.</param>
    /// <exception cref="InvalidOperationException">Объявление не находится на проверке.</exception>
    /// <exception cref="ArgumentException">Причина не указана.</exception>
    public void Reject(string reason)
    {
        if (Status != AdStatus.OnModeration)
        {
            throw new InvalidOperationException("Отклонить можно только объявление на проверке.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Причина отклонения обязательна.", nameof(reason));
        }

        Status = AdStatus.Rejected;
        ModerationReason = reason.Trim();
    }

    /// <summary>
    /// Скрывает активное объявление с обязательной причиной.
    /// </summary>
    /// <param name="reason">Причина скрытия.</param>
    /// <exception cref="InvalidOperationException">Объявление не активно.</exception>
    /// <exception cref="ArgumentException">Причина не указана.</exception>
    public void Hide(string reason)
    {
        if (Status != AdStatus.Active)
        {
            throw new InvalidOperationException("Скрыть можно только активное объявление.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Причина скрытия обязательна.", nameof(reason));
        }

        Status = AdStatus.Hidden;
        ModerationReason = reason.Trim();
    }

    /// <summary>Срок публикации объявления в днях.</summary>
    public const int LifetimeDays = 7;

    /// <summary>
    /// Завершает редактирование: непроверенный автор возвращает объявление на проверку,
    /// доверенный сохраняет текущий статус.
    /// </summary>
    /// <param name="trustedAuthor">Признак доверия автора.</param>
    public void CompleteEdit(bool trustedAuthor)
    {
        if (trustedAuthor || Status == AdStatus.OnModeration)
        {
            return;
        }

        Status = AdStatus.OnModeration;
        ModerationReason = null;
        ReminderSentAt = null;
    }

    /// <summary>Отмечает активное объявление проданным.</summary>
    /// <exception cref="InvalidOperationException">Объявление не активно.</exception>
    public void MarkSold()
    {
        if (Status != AdStatus.Active)
        {
            throw new InvalidOperationException("Продать можно только активное объявление.");
        }

        Status = AdStatus.Sold;
    }

    /// <summary>
    /// Возвращает проданное объявление в продажу: доверенный автор публикует сразу,
    /// непроверенный — отправляет на проверку.
    /// </summary>
    /// <param name="trustedAuthor">Признак доверия автора.</param>
    /// <param name="now">Момент возврата в продажу.</param>
    /// <exception cref="InvalidOperationException">Объявление не продано.</exception>
    public void ReturnToSale(bool trustedAuthor, DateTimeOffset now)
    {
        if (Status != AdStatus.Sold)
        {
            throw new InvalidOperationException("Вернуть в продажу можно только проданное объявление.");
        }

        if (trustedAuthor)
        {
            Status = AdStatus.Active;
            PublishedAt = now;
            ReminderSentAt = null;
            return;
        }

        Status = AdStatus.OnModeration;
        ModerationReason = null;
        ReminderSentAt = null;
    }

    /// <summary>Переводит активное объявление в архив.</summary>
    /// <exception cref="InvalidOperationException">Объявление не активно.</exception>
    public void Archive()
    {
        if (Status != AdStatus.Active)
        {
            throw new InvalidOperationException("Архивировать можно только активное объявление.");
        }

        Status = AdStatus.Archived;
    }

    /// <summary>
    /// Продлевает активное или архивное объявление на новый срок и возвращает его в ленту.
    /// </summary>
    /// <param name="now">Момент продления.</param>
    /// <exception cref="InvalidOperationException">Объявление нельзя продлить.</exception>
    public void Renew(DateTimeOffset now)
    {
        if (Status is not (AdStatus.Active or AdStatus.Archived))
        {
            throw new InvalidOperationException(
                "Продлить можно только активное или архивное объявление.");
        }

        Status = AdStatus.Active;
        PublishedAt = now;
        ReminderSentAt = null;
    }
}
