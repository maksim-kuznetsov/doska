namespace Doska.Domain.Entities;

/// <summary>
/// Категория каталога: либо раздел (без родителя), либо подраздел.
/// Глубина ограничена двумя уровнями, поэтому подраздел не может быть родителем.
/// </summary>
public class Category : Entity
{
    /// <summary>Название раздела или подраздела; уникально.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Родительский раздел; <c>null</c> — это раздел верхнего уровня.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Родительский раздел.</summary>
    public Category? Parent { get; set; }

    /// <summary>Подразделы раздела.</summary>
    public ICollection<Category> Children { get; set; } = new List<Category>();

    /// <summary>Признак доступности категории для выбора.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Порядок вывода в списке; неотрицательное число.</summary>
    public int SortOrder { get; set; }

    /// <summary>Объявления категории.</summary>
    public ICollection<Ad> Ads { get; set; } = new List<Ad>();

    /// <summary>Признак раздела верхнего уровня.</summary>
    public bool IsSection => ParentId is null;

    /// <summary>
    /// Назначает родителя, соблюдая правило «подраздел не может быть родителем»:
    /// глубина каталога ограничена двумя уровнями.
    /// </summary>
    /// <param name="parent">Раздел верхнего уровня или <c>null</c>, чтобы сделать категорию разделом.</param>
    /// <exception cref="InvalidOperationException">Родитель сам является подразделом или совпадает с категорией.</exception>
    public void SetParent(Category? parent)
    {
        if (parent is null)
        {
            Parent = null;
            ParentId = null;
            return;
        }

        if (parent.Id == Id)
        {
            throw new InvalidOperationException("Категория не может быть родителем самой себя.");
        }

        if (!parent.IsSection)
        {
            throw new InvalidOperationException(
                "Подраздел не может быть родителем: каталог ограничен двумя уровнями.");
        }

        Parent = parent;
        ParentId = parent.Id;
    }
}
