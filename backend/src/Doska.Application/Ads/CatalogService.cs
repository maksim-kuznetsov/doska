using Doska.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Ads;

/// <summary>Город для выбора при подаче объявления.</summary>
public sealed record CityOption(Guid Id, string Name);

/// <summary>Подраздел категории для выбора при подаче объявления.</summary>
public sealed record CategoryOption(Guid Id, string Name, Guid SectionId, string SectionName);

/// <summary>Подраздел в дереве разделов для фильтра ленты.</summary>
public sealed record SubsectionOption(Guid Id, string Name);

/// <summary>Раздел верхнего уровня с активными подразделами.</summary>
public sealed record SectionOption(Guid Id, string Name, IReadOnlyList<SubsectionOption> Subsections);

/// <summary>Справочники городов и категорий.</summary>
public sealed class CatalogService(IAppDbContext db)
{
    /// <summary>Активные города.</summary>
    public async Task<IReadOnlyList<CityOption>> GetCitiesAsync(CancellationToken cancellationToken = default)
        => await db.Cities
            .Where(city => city.IsActive)
            .OrderBy(city => city.Name)
            .Select(city => new CityOption(city.Id, city.Name))
            .ToListAsync(cancellationToken);

    /// <summary>Активные подразделы категорий вместе с названием раздела.</summary>
    public async Task<IReadOnlyList<CategoryOption>> GetSubsectionsAsync(CancellationToken cancellationToken = default)
        => await db.Categories
            .Where(category => category.IsActive && category.ParentId != null)
            .OrderBy(category => category.Parent!.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryOption(
                category.Id,
                category.Name,
                category.ParentId!.Value,
                category.Parent!.Name))
            .ToListAsync(cancellationToken);

    /// <summary>Активные разделы верхнего уровня вместе с активными подразделами.</summary>
    public async Task<IReadOnlyList<SectionOption>> GetSectionsAsync(
        CancellationToken cancellationToken = default)
    {
        var sections = await db.Categories
            .Where(category => category.IsActive && category.ParentId == null)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new { category.Id, category.Name })
            .ToListAsync(cancellationToken);

        var sectionIds = sections.Select(section => section.Id).ToList();

        var subsections = await db.Categories
            .Where(category =>
                category.IsActive
                && category.ParentId != null
                && sectionIds.Contains(category.ParentId!.Value))
            .OrderBy(category => category.Name)
            .Select(category => new
            {
                category.Id,
                category.Name,
                SectionId = category.ParentId!.Value,
            })
            .ToListAsync(cancellationToken);

        return sections
            .Select(section => new SectionOption(
                section.Id,
                section.Name,
                subsections
                    .Where(subsection => subsection.SectionId == section.Id)
                    .Select(subsection => new SubsectionOption(subsection.Id, subsection.Name))
                    .ToList()))
            .ToList();
    }
}