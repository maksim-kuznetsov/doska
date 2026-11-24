namespace Doska.Domain.Entities;

/// <summary>
/// Базовая сущность: идентификатор и отметки времени.
/// Отметки времени принадлежат домену: конструктор фиксирует создание,
/// а доменные методы отмечают изменение. Слой доступа к данным их не проставляет.
/// </summary>
public abstract class Entity
{
    /// <summary>Идентификатор в базе: <c>uuid</c>, значение версии 7 (упорядочено по времени).</summary>
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    /// <summary>Момент создания записи (UTC, <c>timestamptz</c>).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Момент последнего изменения записи (UTC, <c>timestamptz</c>).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Создаёт сущность без отметок времени. Используется EF Core при чтении из базы,
    /// где значения восстанавливаются из колонок.
    /// </summary>
    protected Entity()
    {
    }

    /// <summary>Создаёт сущность и фиксирует момент создания.</summary>
    /// <param name="now">Момент создания.</param>
    protected Entity(DateTimeOffset now)
    {
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>Отмечает изменение сущности указанным моментом.</summary>
    /// <param name="now">Момент изменения.</param>
    protected void MarkUpdated(DateTimeOffset now) => UpdatedAt = now;
}
