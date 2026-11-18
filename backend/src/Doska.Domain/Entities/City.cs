namespace Doska.Domain.Entities;

/// <summary>Город, в котором действует площадка.</summary>
public class City : Entity
{
    /// <summary>Предельная длина названия города.</summary>
    public const int MaxNameLength = 200;

    /// <summary>Название города; уникально.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Признак доступности города для выбора в объявлениях.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Объявления города.</summary>
    public ICollection<Ad> Ads { get; set; } = new List<Ad>();

    /// <summary>Переименовывает город.</summary>
    /// <param name="name">Новое название.</param>
    /// <exception cref="ArgumentException">Название пустое или слишком длинное.</exception>
    public void Rename(string name)
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
    }

    /// <summary>Делает город доступным для выбора.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Скрывает город из выбора, не затрагивая объявления.</summary>
    public void Deactivate() => IsActive = false;
}