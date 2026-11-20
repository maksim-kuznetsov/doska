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
    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        await ReferenceDataSeeder.SeedAsync(db, cancellationToken);
        await EnsureUsersAsync(db, passwordHasher, cancellationToken);
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