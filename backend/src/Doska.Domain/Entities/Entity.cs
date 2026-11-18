namespace Doska.Domain.Entities;

/// <summary>
/// Базовая сущность: идентификатор и отметки времени.
/// Отметки времени заполняет <c>AppDbContext</c> при сохранении через <c>TimeProvider</c>,
/// поэтому сущность не зависит от системных часов.
/// </summary>
public abstract class Entity
{
    /// <summary>Идентификатор в базе: <c>uuid</c>, значение версии 7 (упорядочено по времени).</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Момент создания записи (UTC, <c>timestamptz</c>).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Момент последнего изменения записи (UTC, <c>timestamptz</c>).</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
