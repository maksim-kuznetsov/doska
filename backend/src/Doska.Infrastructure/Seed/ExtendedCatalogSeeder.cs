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
        TimeProvider clock,
        CancellationToken cancellationToken = default)
    {
        var citiesAdded = await EnsureCitiesAsync(db, clock, cancellationToken);
        var (sectionsAdded, subsectionsAdded, skipped) =
            await EnsureCategoriesAsync(db, clock, cancellationToken);

        return new CatalogSeedResult(citiesAdded, sectionsAdded, subsectionsAdded, skipped);
    }

    private static async Task<int> EnsureCitiesAsync(
        AppDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
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

            db.Cities.Add(new City(name, clock.GetUtcNow()));
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return added;
    }

    private static async Task<(int Sections, int Subsections, IReadOnlyList<string> Skipped)>
        EnsureCategoriesAsync(
            AppDbContext db,
            TimeProvider clock,
            CancellationToken cancellationToken)
    {
        // Название категории уникально глобально, поэтому карта «название → категория»
        // полностью описывает занятые имена.
        var categories = await db.Categories.ToListAsync(cancellationToken);

        var known = new Dictionary<string, Category>(StringComparer.Ordinal);

        foreach (var category in categories)
        {
            known[category.Name] = category;
        }

        var sectionsAdded = 0;
        var subsectionsAdded = 0;
        var skipped = new List<string>();
        var sectionOrder = 0;

        foreach (var (sectionName, subsections) in CatalogSeedData.Sections)
        {
            sectionOrder++;

            Category section;

            if (known.TryGetValue(sectionName, out var existingSection))
            {
                if (!existingSection.IsSection)
                {
                    // Имя занято подразделом: раздел с таким названием создать нельзя.
                    skipped.Add(sectionName);
                    continue;
                }

                section = existingSection;
            }
            else
            {
                section = new Category(sectionName, sectionOrder, clock.GetUtcNow());
                db.Categories.Add(section);
                await db.SaveChangesAsync(cancellationToken);

                known[sectionName] = section;
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

                var subsection = new Category(subsectionName, subsectionOrder, section, clock.GetUtcNow());
                db.Categories.Add(subsection);
                await db.SaveChangesAsync(cancellationToken);

                known[subsectionName] = subsection;
                subsectionsAdded++;
            }
        }

        return (sectionsAdded, subsectionsAdded, skipped);
    }
}
