using Doska.Application.Abstractions;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Categories;

/// <summary>Подраздел в административном дереве.</summary>
public sealed record CategoryItemView(Guid Id, string Name, int SortOrder, bool IsActive);

/// <summary>Раздел с подразделами в административном дереве.</summary>
public sealed record CategorySectionView(
    Guid Id,
    string Name,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<CategoryItemView> Subsections);

/// <summary>Управление разделами и подразделами каталога.</summary>
public sealed class CategoryService(IAppDbContext db)
{
    /// <summary>Полное дерево для администратора, включая неактивные элементы.</summary>
    public async Task<IReadOnlyList<CategorySectionView>> GetAdminTreeAsync(
        CancellationToken cancellationToken = default)
    {
        var sections = await db.Categories
            .Where(category => category.ParentId == null)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new
            {
                category.Id,
                category.Name,
                category.SortOrder,
                category.IsActive,
            })
            .ToListAsync(cancellationToken);

        var sectionIds = sections.Select(section => section.Id).ToList();

        var subsections = await db.Categories
            .Where(category =>
                category.ParentId != null
                && sectionIds.Contains(category.ParentId!.Value))
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new
            {
                category.Id,
                category.Name,
                category.SortOrder,
                category.IsActive,
                SectionId = category.ParentId!.Value,
            })
            .ToListAsync(cancellationToken);

        return sections
            .Select(section => new CategorySectionView(
                section.Id,
                section.Name,
                section.SortOrder,
                section.IsActive,
                subsections
                    .Where(subsection => subsection.SectionId == section.Id)
                    .Select(subsection => new CategoryItemView(
                        subsection.Id,
                        subsection.Name,
                        subsection.SortOrder,
                        subsection.IsActive))
                    .ToList()))
            .ToList();
    }

    /// <summary>Создаёт раздел верхнего уровня.</summary>
    public async Task<CategorySectionView> CreateSectionAsync(
        string name,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = await NormalizeAvailableNameAsync(name, null, cancellationToken);
        var section = Category.CreateSection(normalizedName, sortOrder);

        db.Categories.Add(section);
        await db.SaveChangesAsync(cancellationToken);

        return new CategorySectionView(
            section.Id,
            section.Name,
            section.SortOrder,
            section.IsActive,
            Array.Empty<CategoryItemView>());
    }

    /// <summary>Создаёт подраздел в разделе верхнего уровня.</summary>
    public async Task<CategoryItemView> CreateSubsectionAsync(
        Guid sectionId,
        string name,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var section = await LoadAsync(sectionId, cancellationToken);

        if (!section.IsSection)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Подраздел можно создать только в разделе верхнего уровня.");
        }

        var normalizedName = await NormalizeAvailableNameAsync(name, null, cancellationToken);
        var subsection = Category.CreateSubsection(section, normalizedName, sortOrder);

        db.Categories.Add(subsection);
        await db.SaveChangesAsync(cancellationToken);

        return ToItemView(subsection);
    }

    /// <summary>Переименовывает категорию и меняет порядок вывода.</summary>
    public async Task<CategoryItemView> UpdateAsync(
        Guid id,
        string name,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var category = await LoadAsync(id, cancellationToken);
        var normalizedName = await NormalizeAvailableNameAsync(name, id, cancellationToken);

        category.Rename(normalizedName);
        category.SetSortOrder(sortOrder);

        await db.SaveChangesAsync(cancellationToken);
        return ToItemView(category);
    }

    /// <summary>Делает категорию доступной для выбора.</summary>
    public async Task<CategoryItemView> ActivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await LoadAsync(id, cancellationToken);
        category.Activate();
        await db.SaveChangesAsync(cancellationToken);
        return ToItemView(category);
    }

    /// <summary>Скрывает категорию из выбора, не затрагивая объявления.</summary>
    public async Task<CategoryItemView> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await LoadAsync(id, cancellationToken);
        category.Deactivate();
        await db.SaveChangesAsync(cancellationToken);
        return ToItemView(category);
    }

    /// <summary>
    /// Удаляет категорию, если у неё нет объявлений и подразделов.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await db.Categories
            .Include(candidate => candidate.Children)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.NotFound, "Категория не найдена.");

        if (category.Children.Count > 0)
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Нельзя удалить раздел с подразделами. Деактивируйте его.");
        }

        var hasAds = await db.Ads.AnyAsync(ad => ad.CategoryId == id, cancellationToken);

        if (hasAds)
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Нельзя удалить категорию с объявлениями. Деактивируйте её.");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> LoadAsync(Guid id, CancellationToken cancellationToken)
        => await db.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.NotFound, "Категория не найдена.");

    private async Task<string> NormalizeAvailableNameAsync(
        string name,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var trimmed = (name ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Название категории обязательно.");
        }

        if (trimmed.Length > Category.MaxNameLength)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                $"Название категории не длиннее {Category.MaxNameLength} символов.");
        }

        var query = db.Categories.Where(category => category.Name == trimmed);

        if (exceptId is Guid id)
        {
            query = query.Where(category => category.Id != id);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Категория с таким названием уже существует.");
        }

        return trimmed;
    }

    private static CategoryItemView ToItemView(Category category)
        => new(category.Id, category.Name, category.SortOrder, category.IsActive);
}