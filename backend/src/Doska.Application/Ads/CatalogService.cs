using Doska.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Ads;

/// <summary>Город для выбора при подаче объявления.</summary>
public sealed record CityOption(Guid Id, string Name);

/// <summary>Подраздел категории для выбора при подаче объявления.</summary>
public sealed record CategoryOption(Guid Id, string Name, Guid SectionId, string SectionName);

/// <summary>Справочники городов и подразделов категорий.</summary>
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
}