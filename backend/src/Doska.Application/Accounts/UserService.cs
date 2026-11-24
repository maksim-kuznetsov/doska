using Doska.Application.Abstractions;
using Doska.Application.Common;
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
    bool IsEmailConfirmed);

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
                user.IsEmailConfirmed))
            .ToListAsync(cancellationToken);

    /// <summary>Меняет роль пользователя.</summary>
    public async Task<AdminUserView> ChangeRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Неизвестная роль пользователя.");
        }

        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.NotFound, "Пользователь не найден.");

        user.ChangeRole(parsed, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        return new AdminUserView(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.IsTrusted,
            user.IsBlocked,
            user.IsEmailConfirmed);
    }
}