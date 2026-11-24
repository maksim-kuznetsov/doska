namespace Doska.Domain.Entities;

/// <summary>
/// Категория каталога: либо раздел (без родителя), либо подраздел.
/// Глубина ограничена двумя уровнями, поэтому подраздел не может быть родителем.
/// </summary>
public class Category : Entity
{
    /// <summary>Максимальная длина названия категории.</summary>
    public const int MaxNameLength = 200;

    /// <summary>Название раздела или подраздела; уникально.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Родительский раздел; <c>null</c> — это раздел верхнего уровня.</summary>
    public Guid? ParentId { get; private set; }

    /// <summary>Родительский раздел.</summary>
    public Category? Parent { get; private set; }

    /// <summary>Подразделы раздела.</summary>
    public ICollection<Category> Children { get; } = new List<Category>();

    /// <summary>Признак доступности категории для выбора.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Порядок вывода в списке; неотрицательное число.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Объявления категории.</summary>
    public ICollection<Ad> Ads { get; } = new List<Ad>();

    /// <summary>Признак раздела верхнего уровня.</summary>
    public bool IsSection => ParentId is null;

    /// <summary>Создаёт пустую категорию: используется EF Core при чтении из базы.</summary>
    private Category()
    {
    }

    /// <summary>Создаёт раздел верхнего уровня.</summary>
    /// <param name="name">Название раздела.</param>
    /// <param name="sortOrder">Порядок вывода.</param>
    /// <param name="now">Момент создания.</param>
    public Category(string name, int sortOrder, DateTimeOffset now)
        : base(now)
    {
        Rename(name, now);
        SetSortOrder(sortOrder, now);
    }

    /// <summary>Создаёт подраздел в разделе верхнего уровня.</summary>
    /// <param name="name">Название подраздела.</param>
    /// <param name="sortOrder">Порядок вывода.</param>
    /// <param name="section">Раздел верхнего уровня.</param>
    /// <param name="now">Момент создания.</param>
    /// <exception cref="InvalidOperationException">Родитель не является разделом.</exception>
    public Category(string name, int sortOrder, Category section, DateTimeOffset now)
        : base(now)
    {
        Rename(name, now);
        SetSortOrder(sortOrder, now);
        SetParent(section, now);
    }

    /// <summary>Переименовывает категорию.</summary>
    /// <param name="name">Новое название.</param>
    /// <param name="now">Момент изменения.</param>
    /// <exception cref="ArgumentException">Название пустое или слишком длинное.</exception>
    public void Rename(string name, DateTimeOffset now)
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
        MarkUpdated(now);
    }

    /// <summary>Задаёт порядок вывода в списке.</summary>
    /// <param name="sortOrder">Неотрицательный порядок.</param>
    /// <param name="now">Момент изменения.</param>
    /// <exception cref="ArgumentOutOfRangeException">Порядок отрицательный.</exception>
    public void SetSortOrder(int sortOrder, DateTimeOffset now)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Порядок вывода не может быть отрицательным.");
        }

        SortOrder = sortOrder;
        MarkUpdated(now);
    }

    /// <summary>Делает категорию доступной для выбора.</summary>
    /// <param name="now">Момент изменения.</param>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        MarkUpdated(now);
    }

    /// <summary>Скрывает категорию из выбора, не затрагивая существующие объявления.</summary>
    /// <param name="now">Момент изменения.</param>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        MarkUpdated(now);
    }

    /// <summary>
    /// Назначает родителя, соблюдая правило «подраздел не может быть родителем»:
    /// глубина каталога ограничена двумя уровнями.
    /// </summary>
    /// <param name="parent">Раздел верхнего уровня или <c>null</c>, чтобы сделать категорию разделом.</param>
    /// <param name="now">Момент изменения.</param>
    /// <exception cref="InvalidOperationException">Родитель сам является подразделом или совпадает с категорией.</exception>
    public void SetParent(Category? parent, DateTimeOffset now)
    {
        if (parent is null)
        {
            Parent = null;
            ParentId = null;
            MarkUpdated(now);
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
        MarkUpdated(now);
    }
}
