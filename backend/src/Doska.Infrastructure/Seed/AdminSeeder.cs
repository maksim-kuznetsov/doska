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
    /// <returns><c>true</c>, если пользователь создан, и <c>false</c>, если обновлён.</returns>
    public static async Task<bool> CreateOrUpdateAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();
        var user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Email.ToLower() == normalizedEmail.ToLower(),
            cancellationToken);

        if (user is null)
        {
            db.Users.Add(new User
            {
                Email = normalizedEmail,
                PasswordHash = passwordHasher.Hash(password),
                Role = UserRole.Admin,
                IsEmailConfirmed = true,
                IsTrusted = false,
            });

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        user.Role = UserRole.Admin;
        user.IsEmailConfirmed = true;
        user.PasswordHash = passwordHasher.Hash(password);
        await db.SaveChangesAsync(cancellationToken);
        return false;
    }
}