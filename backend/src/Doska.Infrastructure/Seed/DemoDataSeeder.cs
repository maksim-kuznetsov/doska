using Doska.Application.Abstractions;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Doska.Infrastructure.Seed;

/// <summary>
/// Наполняет базу демонстрационными данными: справочники и три учётные записи
/// (пользователь, модератор, администратор). Идемпотентен. Только для разработки.
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
    /// <param name="db">Контекст базы данных.</param>
    /// <param name="passwordHasher">Хеширование паролей.</param>
    /// <param name="clock">Источник времени для отметок создания.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        TimeProvider clock,
        CancellationToken cancellationToken = default)
    {
        await ReferenceDataSeeder.SeedAsync(db, clock, cancellationToken);
        await EnsureUsersAsync(db, passwordHasher, clock, cancellationToken);
    }

    private static async Task EnsureUsersAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        foreach (var (email, role) in DemoUsers)
        {
            if (await db.Users.AnyAsync(
                    user => user.Email.ToLower() == email.ToLower(),
                    cancellationToken))
            {
                continue;
            }

            var user = new User(email, passwordHasher.Hash(DemoPassword), role, now);
            user.ConfirmEmail(now);
            db.Users.Add(user);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
