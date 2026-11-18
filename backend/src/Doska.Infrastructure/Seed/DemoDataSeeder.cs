using Doska.Application.Abstractions;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Doska.Infrastructure.Seed;

/// <summary>
/// Наполняет базу демонстрационными данными: города, категории и три учётные записи
/// (пользователь, модератор, администратор). Идемпотентен.
/// </summary>
public static class DemoDataSeeder
{
    /// <summary>Пароль демонстрационных учётных записей.</summary>
    public const string DemoPassword = "123789";

    /// <summary>Демонстрационные учётные записи.</summary>
    public static readonly (string Email, UserRole Role)[] DemoUsers =
    [
        ("user@example.com", UserRole.User),
        ("moderator@example.com", UserRole.Moderator),
        ("admin@example.com", UserRole.Admin),
    ];

    /// <summary>Добавляет демонстрационные данные, если их ещё нет.</summary>
    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        await EnsureCitiesAsync(db, cancellationToken);
        await EnsureCategoriesAsync(db, cancellationToken);
        await EnsureUsersAsync(db, passwordHasher, cancellationToken);
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

    private static async Task EnsureUsersAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken)
    {
        foreach (var (email, role) in DemoUsers)
        {
            if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
            {
                continue;
            }

            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = passwordHasher.Hash(DemoPassword),
                Role = role,
                IsEmailConfirmed = true,
                IsTrusted = false,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
