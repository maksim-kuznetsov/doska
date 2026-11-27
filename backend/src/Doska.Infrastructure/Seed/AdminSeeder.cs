using Doska.Application.Abstractions;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Doska.Infrastructure.Seed;

/// <summary>Создаёт администратора production или повышает существующего пользователя.</summary>
public static class AdminSeeder
{
    /// <summary>
    /// Создаёт подтверждённого администратора с указанным паролем. Если пользователь
    /// с таким email уже есть, обновляет роль, подтверждение и пароль.
    /// </summary>
    /// <param name="db">Контекст базы данных.</param>
    /// <param name="passwordHasher">Хеширование паролей.</param>
    /// <param name="email">Адрес администратора.</param>
    /// <param name="password">Пароль администратора.</param>
    /// <param name="clock">Источник времени для отметок создания и изменения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns><c>true</c>, если пользователь создан, и <c>false</c>, если обновлён.</returns>
    public static async Task<bool> CreateOrUpdateAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        string email,
        string password,
        TimeProvider clock,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var now = clock.GetUtcNow();
        var user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            var created = new User(normalizedEmail, passwordHasher.Hash(password), UserRole.Admin, now);
            created.ConfirmEmail(now);
            db.Users.Add(created);

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        user.ChangeRole(UserRole.Admin, now);
        user.ConfirmEmail(now);
        user.SetPassword(passwordHasher.Hash(password), now);
        await db.SaveChangesAsync(cancellationToken);
        return false;
    }
}
