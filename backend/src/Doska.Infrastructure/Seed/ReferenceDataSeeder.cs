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
    /// <param name="db">Контекст базы данных.</param>
    /// <param name="clock">Источник времени для отметок создания.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public static async Task SeedAsync(
        AppDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken = default)
    {
        await EnsureCitiesAsync(db, clock, cancellationToken);
        await EnsureCategoriesAsync(db, clock, cancellationToken);
    }

    private static async Task EnsureCitiesAsync(
        AppDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var names = new[] { "Москва", "Санкт-Петербург", "Новосибирск" };

        foreach (var name in names)
        {
            if (!await db.Cities.AnyAsync(city => city.Name == name, cancellationToken))
            {
                db.Cities.Add(new City(name, clock.GetUtcNow()));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCategoriesAsync(
        AppDbContext db,
        TimeProvider clock,
        CancellationToken cancellationToken)
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
                category => category.Name == sectionName,
                cancellationToken);

            if (section is not null && section.ParentId is not null)
            {
                // Имя уже занято подразделом: пропускаем раздел, чтобы не нарушить
                // глобальную уникальность названий категорий.
                continue;
            }

            if (section is null)
            {
                section = new Category(sectionName, sortOrder, clock.GetUtcNow());
                db.Categories.Add(section);
                await db.SaveChangesAsync(cancellationToken);
            }

            foreach (var subsectionName in subsections)
            {
                var exists = await db.Categories.AnyAsync(
                    category => category.Name == subsectionName,
                    cancellationToken);

                if (!exists)
                {
                    db.Categories.Add(new Category(subsectionName, 0, section, clock.GetUtcNow()));
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
