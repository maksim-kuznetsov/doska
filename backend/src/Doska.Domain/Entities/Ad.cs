using Doska.Domain.Enums;

namespace Doska.Domain.Entities;

/// <summary>Объявление.</summary>
public class Ad : Entity
{
    /// <summary>Срок публикации объявления в днях.</summary>
    public const int LifetimeDays = 7;

    /// <summary>Автор объявления.</summary>
    public Guid AuthorId { get; private set; }

    /// <summary>Автор объявления.</summary>
    public User? Author { get; private set; }

    /// <summary>Категория объявления: всегда подраздел.</summary>
    public Guid CategoryId { get; private set; }

    /// <summary>Категория объявления.</summary>
    public Category? Category { get; private set; }

    /// <summary>Город объявления.</summary>
    public Guid CityId { get; private set; }

    /// <summary>Город объявления.</summary>
    public City? City { get; private set; }

    /// <summary>Заголовок объявления.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Описание объявления.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Цена; неотрицательное значение.</summary>
    public decimal Price { get; private set; }

    /// <summary>Контактные данные автора в свободной форме.</summary>
    public string Contacts { get; private set; } = string.Empty;

    /// <summary>Текущее состояние объявления.</summary>
    public AdStatus Status { get; private set; } = AdStatus.OnModeration;

    /// <summary>Причина отклонения или скрытия; заполняется модератором.</summary>
    public string? ModerationReason { get; private set; }

    /// <summary>
    /// Начало текущего срока публикации. Отдельного поля окончания нет:
    /// объявление уходит в архив, когда прошло больше семи суток с этого момента.
    /// </summary>
    public DateTimeOffset PublishedAt { get; private set; }

    /// <summary>Момент отправки напоминания об истечении срока; <c>null</c> — напоминание не отправлено.</summary>
    public DateTimeOffset? ReminderSentAt { get; private set; }

    /// <summary>Фотографии объявления; не более пяти.</summary>
    public ICollection<AdPhoto> Photos { get; } = new List<AdPhoto>();

    /// <summary>Жалобы на объявление.</summary>
    public ICollection<AdComplaint> Complaints { get; } = new List<AdComplaint>();

    /// <summary>Создаёт пустое объявление: используется EF Core при чтении из базы.</summary>
    private Ad()
    {
    }

    /// <summary>
    /// Создаёт объявление. Статус зависит от доверия автора: доверенный публикуется
    /// сразу, недоверенный отправляется на проверку.
    /// </summary>
    /// <param name="authorId">Автор объявления.</param>
    /// <param name="categoryId">Подраздел категории.</param>
    /// <param name="cityId">Город.</param>
    /// <param name="title">Заголовок.</param>
    /// <param name="description">Описание.</param>
    /// <param name="price">Цена.</param>
    /// <param name="contacts">Контактные данные.</param>
    /// <param name="trustedAuthor">Признак доверия автора.</param>
    /// <param name="now">Момент подачи.</param>
    public Ad(
        Guid authorId,
        Guid categoryId,
        Guid cityId,
        string title,
        string description,
        decimal price,
        string contacts,
        bool trustedAuthor,
        DateTimeOffset now)
        : base(now)
    {
        ValidatePrice(price);

        AuthorId = authorId;
        CategoryId = categoryId;
        CityId = cityId;
        Title = title;
        Description = description;
        Price = price;
        Contacts = contacts;
        Status = trustedAuthor ? AdStatus.Active : AdStatus.OnModeration;
        PublishedAt = trustedAuthor ? now : default;
    }

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
        MarkUpdated(now);
    }

    /// <summary>
    /// Отклоняет объявление, находящееся на проверке, с обязательной причиной.
    /// </summary>
    /// <param name="reason">Причина отклонения.</param>
    /// <param name="now">Момент принятия решения.</param>
    /// <exception cref="InvalidOperationException">Объявление не находится на проверке.</exception>
    /// <exception cref="ArgumentException">Причина не указана.</exception>
    public void Reject(string reason, DateTimeOffset now)
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
        MarkUpdated(now);
    }

    /// <summary>
    /// Скрывает активное объявление с обязательной причиной.
    /// </summary>
    /// <param name="reason">Причина скрытия.</param>
    /// <param name="now">Момент скрытия.</param>
    /// <exception cref="InvalidOperationException">Объявление не активно.</exception>
    /// <exception cref="ArgumentException">Причина не указана.</exception>
    public void Hide(string reason, DateTimeOffset now)
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
        MarkUpdated(now);
    }

    /// <summary>
    /// Завершает редактирование: непроверенный автор возвращает объявление на проверку,
    /// доверенный сохраняет текущий статус.
    /// </summary>
    /// <param name="trustedAuthor">Признак доверия автора.</param>
    /// <param name="now">Момент завершения редактирования.</param>
    public void CompleteEdit(bool trustedAuthor, DateTimeOffset now)
    {
        if (Status is not (AdStatus.Active or AdStatus.Archived or AdStatus.Sold))
        {
            throw new InvalidOperationException(
                "Редактировать можно только активное, архивное или проданное объявление.");
        }

        if (trustedAuthor)
        {
            return;
        }

        Status = AdStatus.OnModeration;
        ModerationReason = null;
        ReminderSentAt = null;
        MarkUpdated(now);
    }

    /// <summary>
    /// Изменяет содержимое объявления и завершает редактирование по уровню доверия автора.
    /// </summary>
    /// <param name="title">Новый заголовок.</param>
    /// <param name="description">Новое описание.</param>
    /// <param name="price">Новая цена.</param>
    /// <param name="contacts">Новые контактные данные.</param>
    /// <param name="cityId">Новый город.</param>
    /// <param name="categoryId">Новый подраздел категории.</param>
    /// <param name="trustedAuthor">Признак доверия автора.</param>
    /// <param name="now">Момент изменения.</param>
    public void UpdateDetails(
        string title,
        string description,
        decimal price,
        string contacts,
        Guid cityId,
        Guid categoryId,
        bool trustedAuthor,
        DateTimeOffset now)
    {
        ValidatePrice(price);

        Title = title;
        Description = description;
        Price = price;
        Contacts = contacts;
        CityId = cityId;
        CategoryId = categoryId;
        CompleteEdit(trustedAuthor, now);

        if (trustedAuthor)
        {
            MarkUpdated(now);
        }
    }

    /// <summary>Отмечает активное объявление проданным.</summary>
    /// <param name="now">Момент продажи.</param>
    /// <exception cref="InvalidOperationException">Объявление не активно.</exception>
    public void MarkSold(DateTimeOffset now)
    {
        if (Status != AdStatus.Active)
        {
            throw new InvalidOperationException("Продать можно только активное объявление.");
        }

        Status = AdStatus.Sold;
        MarkUpdated(now);
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
            MarkUpdated(now);
            return;
        }

        Status = AdStatus.OnModeration;
        ModerationReason = null;
        ReminderSentAt = null;
        MarkUpdated(now);
    }

    /// <summary>Переводит активное объявление в архив.</summary>
    /// <param name="now">Момент архивации.</param>
    /// <exception cref="InvalidOperationException">Объявление не активно.</exception>
    public void Archive(DateTimeOffset now)
    {
        if (Status != AdStatus.Active)
        {
            throw new InvalidOperationException("Архивировать можно только активное объявление.");
        }

        Status = AdStatus.Archived;
        MarkUpdated(now);
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
        MarkUpdated(now);
    }

    /// <summary>Отмечает, что автору отправлено напоминание об истечении срока.</summary>
    /// <param name="now">Момент отправки напоминания.</param>
    public void MarkReminderSent(DateTimeOffset now)
    {
        ReminderSentAt = now;
        MarkUpdated(now);
    }

    private static void ValidatePrice(decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Цена не может быть отрицательной.");
        }
    }
}
