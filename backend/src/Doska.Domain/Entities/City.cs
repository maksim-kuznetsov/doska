namespace Doska.Domain.Entities;

/// <summary>Город, в котором действует площадка.</summary>
public class City : Entity
{
    /// <summary>Название города; уникально.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Признак доступности города для выбора в объявлениях.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Объявления города.</summary>
    public ICollection<Ad> Ads { get; set; } = new List<Ad>();
}
