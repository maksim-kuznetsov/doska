using Doska.Application.Abstractions;
using Doska.Application.Auth;
using Doska.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Accounts;

/// <summary>Ведение профиля текущего пользователя: имя, контакты и смена пароля.</summary>
public sealed class ProfileService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    TimeProvider clock)
{
    /// <summary>Максимальная длина отображаемого имени.</summary>
    public const int MaxDisplayNameLength = 100;

    /// <summary>Максимальная длина контактов; совпадает с ограничением контактов объявления.</summary>
    public const int MaxContactsLength = 500;

    /// <summary>Сохраняет отображаемое имя и контакты пользователя.</summary>
    public async Task<AuthenticatedUser> UpdateAsync(
        Guid userId,
        string? displayName,
        string? contacts,
        CancellationToken cancellationToken = default)
    {
        if (displayName is not null && displayName.Trim().Length > MaxDisplayNameLength)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                $"Отображаемое имя не длиннее {MaxDisplayNameLength} символов.");
        }

        if (contacts is not null && contacts.Trim().Length > MaxContactsLength)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                $"Контакты не длиннее {MaxContactsLength} символов.");
        }

        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.Unauthorized, "Требуется вход.");

        user.UpdateProfile(displayName, contacts, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        return AuthenticatedUser.From(user);
    }

    /// <summary>Меняет пароль пользователя, проверяя текущий пароль.</summary>
    public async Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.Unauthorized, "Требуется вход.");

        if (!passwordHasher.Verify(currentPassword, user.PasswordHash))
        {
            throw new ServiceException(ServiceErrorKind.Unauthorized, "Неверный текущий пароль.");
        }

        AuthService.ValidatePassword(newPassword);
        user.SetPassword(passwordHasher.Hash(newPassword), clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
