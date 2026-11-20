using Doska.Domain.Entities;
using Doska.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Doska.Infrastructure.Seed;

/// <summary>
/// Наполняет базу справочниками — городами и деревом категорий. Идемпотентен:
/// существующие записи не перезаписываются и не дублируются.
/// </summary>
public static class ReferenceDataSeeder
{
    /// <summary>Добавляет города и категории, если их ещё нет.</summary>
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureCitiesAsync(db, cancellationToken);
        await EnsureCategoriesAsync(db, cancellationToken);
    }

    private static async Task EnsureCitiesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var names = new[] { "Москва", "Санкт-Петербург", "Новосибирск" };

        foreach (var name in names)
        {
            if (!await db.Cities.AnyAsync(city => city.Name == name, cancellationToken))
            {
                db.Cities.Add(new City { Name = name, IsActive = true });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCategoriesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var sections = new (string Name, int SortOrder, string[] Subsections)[]
        {
            ("Транспорт", 1, ["Автомобили", "Мотоциклы"]),
            ("Недвижимость", 2, ["Квартиры", "Дома"]),
            ("Электроника", 3, ["Телефоны", "Ноутбуки"]),
        };

        foreach (var (sectionName, sortOrder, subsections) in sections)
        {
            var section = await db.Categories.FirstOrDefaultAsync(
                category => category.Name == sectionName && category.ParentId == null,
                cancellationToken);

            if (section is null)
            {
                section = new Category { Name = sectionName, SortOrder = sortOrder, IsActive = true };
                db.Categories.Add(section);
                await db.SaveChangesAsync(cancellationToken);
            }

            foreach (var subsectionName in subsections)
            {
                var exists = await db.Categories.AnyAsync(
                    category => category.Name == subsectionName && category.ParentId == section.Id,
                    cancellationToken);

                if (!exists)
                {
                    db.Categories.Add(new Category
                    {
                        Name = subsectionName,
                        ParentId = section.Id,
                        IsActive = true,
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}