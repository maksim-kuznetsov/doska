using Doska.Domain.Entities;
using Doska.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Doska.Infrastructure.Seed;

/// <summary>Сводка наполнения расширенного справочника.</summary>
/// <param name="CitiesAdded">Сколько городов добавлено.</param>
/// <param name="SectionsAdded">Сколько разделов добавлено.</param>
/// <param name="SubsectionsAdded">Сколько подразделов добавлено.</param>
/// <param name="Skipped">Названия, занятые категорией с другим родителем.</param>
public sealed record CatalogSeedResult(
    int CitiesAdded,
    int SectionsAdded,
    int SubsectionsAdded,
    IReadOnlyList<string> Skipped);

/// <summary>
/// Наполняет базу расширенным справочником городов и категорий (команда <c>seed-catalog</c>).
/// Идемпотентен: создаёт только отсутствующие записи, существующие не перезаписывает,
/// а занятые названия пропускает и сообщает о них в сводке.
/// </summary>
public static class ExtendedCatalogSeeder
{
    /// <summary>Добавляет отсутствующие города и категории расширенного справочника.</summary>
    public static async Task<CatalogSeedResult> SeedAsync(
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        var citiesAdded = await EnsureCitiesAsync(db, cancellationToken);
        var (sectionsAdded, subsectionsAdded, skipped) =
            await EnsureCategoriesAsync(db, cancellationToken);

        return new CatalogSeedResult(citiesAdded, sectionsAdded, subsectionsAdded, skipped);
    }

    private static async Task<int> EnsureCitiesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var existing = await db.Cities
            .Select(city => city.Name)
            .ToListAsync(cancellationToken);
        var known = new HashSet<string>(existing, StringComparer.Ordinal);

        var added = 0;

        foreach (var name in CatalogSeedData.Cities)
        {
            if (!known.Add(name))
            {
                continue;
            }

            db.Cities.Add(new City { Name = name, IsActive = true });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return added;
    }

    private static async Task<(int Sections, int Subsections, IReadOnlyList<string> Skipped)>
        EnsureCategoriesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        // Название категории уникально глобально, поэтому карта «название → (id, родитель)»
        // полностью описывает занятые имена.
        var categories = await db.Categories
            .Select(category => new { category.Id, category.Name, category.ParentId })
            .ToListAsync(cancellationToken);

        var known = new Dictionary<string, (Guid Id, Guid? ParentId)>(StringComparer.Ordinal);

        foreach (var category in categories)
        {
            known[category.Name] = (category.Id, category.ParentId);
        }

        var sectionsAdded = 0;
        var subsectionsAdded = 0;
        var skipped = new List<string>();
        var sectionOrder = 0;

        foreach (var (sectionName, subsections) in CatalogSeedData.Sections)
        {
            sectionOrder++;

            Guid sectionId;

            if (known.TryGetValue(sectionName, out var existingSection))
            {
                if (existingSection.ParentId is not null)
                {
                    // Имя занято подразделом: раздел с таким названием создать нельзя.
                    skipped.Add(sectionName);
                    continue;
                }

                sectionId = existingSection.Id;
            }
            else
            {
                var section = new Category { Name = sectionName, SortOrder = sectionOrder, IsActive = true };
                db.Categories.Add(section);
                await db.SaveChangesAsync(cancellationToken);

                sectionId = section.Id;
                known[sectionName] = (section.Id, null);
                sectionsAdded++;
            }

            var subsectionOrder = 0;

            foreach (var subsectionName in subsections)
            {
                subsectionOrder++;

                if (known.ContainsKey(subsectionName))
                {
                    continue;
                }

                var subsection = new Category
                {
                    Name = subsectionName,
                    ParentId = sectionId,
                    SortOrder = subsectionOrder,
                    IsActive = true,
                };

                db.Categories.Add(subsection);
                await db.SaveChangesAsync(cancellationToken);

                known[subsectionName] = (subsection.Id, sectionId);
                subsectionsAdded++;
            }
        }

        return (sectionsAdded, subsectionsAdded, skipped);
    }
}
