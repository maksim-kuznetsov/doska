using Doska.Application.Abstractions;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Accounts;

/// <summary>Сведения о пользователе для администратора.</summary>
public sealed record AdminUserView(
    Guid Id,
    string Email,
    string Role,
    bool IsTrusted,
    bool IsBlocked,
    bool IsEmailConfirmed,
    string? DisplayName,
    string? Contacts);

/// <summary>Административное управление учётными записями.</summary>
public sealed class UserService(IAppDbContext db, TimeProvider clock)
{
    /// <summary>Список пользователей для администратора.</summary>
    public async Task<IReadOnlyList<AdminUserView>> GetListAsync(
        CancellationToken cancellationToken = default)
        => await db.Users
            .OrderBy(user => user.Email)
            .Select(user => new AdminUserView(
                user.Id,
                user.Email,
                user.Role.ToString(),
                user.IsTrusted,
                user.BlockedAt != null,
                user.IsEmailConfirmed,
                user.DisplayName,
                user.Contacts))
            .ToListAsync(cancellationToken);

    /// <summary>Возвращает сведения о пользователе для администратора.</summary>
    public async Task<AdminUserView> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(userId, cancellationToken);
        return From(user);
    }

    /// <summary>
    /// Сохраняет полный набор изменяемых свойств пользователя. Применяются только
    /// фактически изменившиеся свойства, чтобы ручное сохранение без правок не
    /// двигало отметку изменения и не сбрасывало счётчик доверия.
    /// </summary>
    public async Task<AdminUserView> UpdateAsync(
        Guid userId,
        string role,
        bool isTrusted,
        bool isBlocked,
        bool isEmailConfirmed,
        string? displayName,
        string? contacts,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Неизвестная роль пользователя.");
        }

        var user = await LoadAsync(userId, cancellationToken);
        var now = clock.GetUtcNow();

        if (user.Role != parsed)
        {
            user.ChangeRole(parsed, now);
        }

        if (user.IsTrusted != isTrusted)
        {
            if (isTrusted)
            {
                user.GrantTrust(now);
            }
            else
            {
                user.RevokeTrust(now);
            }
        }

        if (user.IsBlocked != isBlocked)
        {
            if (isBlocked)
            {
                user.Block(now);
            }
            else
            {
                user.Unblock(now);
            }
        }

        if (user.IsEmailConfirmed != isEmailConfirmed)
        {
            user.SetEmailConfirmed(isEmailConfirmed, now);
        }

        var normalizedDisplayName = Normalize(displayName);
        var normalizedContacts = Normalize(contacts);

        if (user.DisplayName != normalizedDisplayName || user.Contacts != normalizedContacts)
        {
            user.UpdateProfile(displayName, contacts, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return From(user);
    }

    private async Task<User> LoadAsync(Guid userId, CancellationToken cancellationToken)
        => await db.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.NotFound, "Пользователь не найден.");

    private static AdminUserView From(User user) => new(
        user.Id,
        user.Email,
        user.Role.ToString(),
        user.IsTrusted,
        user.IsBlocked,
        user.IsEmailConfirmed,
        user.DisplayName,
        user.Contacts);

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
