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

    /// <summary>Максимальная длина названия категории.</summary>
    public const int MaxNameLength = 200;

    /// <summary>Переименовывает категорию.</summary>
    /// <param name="name">Новое название.</param>
    /// <exception cref="ArgumentException">Название пустое или слишком длинное.</exception>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название категории обязательно.", nameof(name));
        }

        var trimmed = name.Trim();

        if (trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Название категории не длиннее {MaxNameLength} символов.",
                nameof(name));
        }

        Name = trimmed;
    }

    /// <summary>Задаёт порядок вывода в списке.</summary>
    /// <param name="sortOrder">Неотрицательный порядок.</param>
    /// <exception cref="ArgumentOutOfRangeException">Порядок отрицательный.</exception>
    public void SetSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Порядок вывода не может быть отрицательным.");
        }

        SortOrder = sortOrder;
    }

    /// <summary>Делает категорию доступной для выбора.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Скрывает категорию из выбора, не затрагивая существующие объявления.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Создаёт раздел верхнего уровня.</summary>
    /// <param name="name">Название раздела.</param>
    /// <param name="sortOrder">Порядок вывода.</param>
    public static Category CreateSection(string name, int sortOrder)
    {
        var section = new Category();
        section.Rename(name);
        section.SetSortOrder(sortOrder);
        return section;
    }

    /// <summary>Создаёт подраздел в разделе верхнего уровня.</summary>
    /// <param name="section">Раздел верхнего уровня.</param>
    /// <param name="name">Название подраздела.</param>
    /// <param name="sortOrder">Порядок вывода.</param>
    /// <exception cref="InvalidOperationException">Родитель не является разделом.</exception>
    public static Category CreateSubsection(Category section, string name, int sortOrder = 0)
    {
        var subsection = new Category();
        subsection.Rename(name);
        subsection.SetSortOrder(sortOrder);
        subsection.SetParent(section);
        return subsection;
    }

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

        if (Children.Count > 0)
        {
            throw new InvalidOperationException(
                "Раздел с подразделами нельзя сделать подразделом.");
        }

        Parent = parent;
        ParentId = parent.Id;
    }
}
