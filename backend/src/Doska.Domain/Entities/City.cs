namespace Doska.Domain.Entities;

/// <summary>Город, в котором действует площадка.</summary>
public class City : Entity
{
    /// <summary>Предельная длина названия города.</summary>
    public const int MaxNameLength = 200;

    /// <summary>Название города; уникально.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Признак доступности города для выбора в объявлениях.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Объявления города.</summary>
    public ICollection<Ad> Ads { get; } = new List<Ad>();

    /// <summary>Создаёт пустой город: используется EF Core при чтении из базы.</summary>
    private City()
    {
    }

    /// <summary>Создаёт город.</summary>
    /// <param name="name">Название города.</param>
    /// <param name="now">Момент создания.</param>
    /// <exception cref="ArgumentException">Название пустое или слишком длинное.</exception>
    public City(string name, DateTimeOffset now)
        : base(now)
        => Rename(name, now);

    /// <summary>Переименовывает город.</summary>
    /// <param name="name">Новое название.</param>
    /// <param name="now">Момент изменения.</param>
    /// <exception cref="ArgumentException">Название пустое или слишком длинное.</exception>
    public void Rename(string name, DateTimeOffset now)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Название города обязательно.", nameof(name));
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Название города не длиннее {MaxNameLength} символов.",
                nameof(name));
        }

        Name = trimmed;
        MarkUpdated(now);
    }

    /// <summary>Делает город доступным для выбора.</summary>
    /// <param name="now">Момент изменения.</param>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        MarkUpdated(now);
    }

    /// <summary>Скрывает город из выбора, не затрагивая объявления.</summary>
    /// <param name="now">Момент изменения.</param>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        MarkUpdated(now);
    }
}
