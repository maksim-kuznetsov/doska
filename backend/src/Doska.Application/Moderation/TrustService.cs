using Doska.Application.Abstractions;
using Doska.Application.Auth;
using Doska.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Moderation;

/// <summary>Ручное управление доверием автора.</summary>
public sealed class TrustService(IAppDbContext db, TimeProvider clock)
{
    /// <summary>Выдаёт доверие пользователю.</summary>
    public async Task<AuthenticatedUser> GrantAsync(Guid userId, CancellationToken cancellationToken = default)
        => await ApplyAsync(userId, grant: true, cancellationToken);

    /// <summary>Отзывает доверие у пользователя.</summary>
    public async Task<AuthenticatedUser> RevokeAsync(Guid userId, CancellationToken cancellationToken = default)
        => await ApplyAsync(userId, grant: false, cancellationToken);

    /// <summary>Блокирует пользователя.</summary>
    public async Task<AuthenticatedUser> BlockAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(userId, cancellationToken);
        user.Block(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return AuthenticatedUser.From(user);
    }

    /// <summary>Снимает блокировку пользователя.</summary>
    public async Task<AuthenticatedUser> UnblockAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(userId, cancellationToken);
        user.Unblock();
        await db.SaveChangesAsync(cancellationToken);
        return AuthenticatedUser.From(user);
    }

    private async Task<AuthenticatedUser> ApplyAsync(
        Guid userId,
        bool grant,
        CancellationToken cancellationToken)
    {
        var user = await LoadAsync(userId, cancellationToken);

        if (grant)
        {
            user.GrantTrust();
        }
        else
        {
            user.RevokeTrust();
        }

        await db.SaveChangesAsync(cancellationToken);
        return AuthenticatedUser.From(user);
    }

    private async Task<Domain.Entities.User> LoadAsync(Guid userId, CancellationToken cancellationToken)
        => await db.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.NotFound, "Пользователь не найден.");
}
