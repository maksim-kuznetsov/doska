using System.Buffers.Text;
using System.Security.Cryptography;
using Doska.Application.Abstractions;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Auth;

/// <summary>
/// Разрешение идентичности внешнего провайдера: вход, автоматическая привязка по email
/// и явная привязка из профиля. Провайдер задаётся идентификатором, поведение общее.
/// </summary>
public sealed class ExternalAuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    TimeProvider clock)
{
    private const int MaxEmailLength = 320;

    /// <summary>
    /// Входит по идентичности внешнего провайдера или создаёт подтверждённый аккаунт
    /// без пароля. Сначала используется сохранённая привязка, затем email и только
    /// потом создаётся аккаунт.
    /// </summary>
    public async Task<AuthenticatedUser> SignInAsync(
        string provider,
        string? subject,
        string? email,
        string? displayName,
        bool emailVerified,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = ExternalLogin.NormalizeProvider(provider);
        var normalizedSubject = ValidateIdentity(subject, email, emailVerified);
        var normalizedEmail = NormalizeEmail(email!);

        var linked = await db.ExternalLogins.FirstOrDefaultAsync(
            login => login.Provider == normalizedProvider && login.Subject == normalizedSubject,
            cancellationToken);

        if (linked is not null)
        {
            var linkedUser = await db.Users.FirstOrDefaultAsync(
                candidate => candidate.Id == linked.UserId,
                cancellationToken);

            if (linkedUser is not null)
            {
                return await ToAuthenticatedAsync(linkedUser, cancellationToken);
            }
        }

        var byEmail = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Email == normalizedEmail,
            cancellationToken);

        if (byEmail is not null)
        {
            if (byEmail.IsBlocked)
            {
                throw new ServiceException(
                    ServiceErrorKind.Forbidden,
                    "Аккаунт заблокирован. Обратитесь к администрации.");
            }

            var existing = await db.ExternalLogins.FirstOrDefaultAsync(
                login => login.UserId == byEmail.Id && login.Provider == normalizedProvider,
                cancellationToken);

            if (existing is not null)
            {
                if (string.Equals(existing.Subject, normalizedSubject, StringComparison.Ordinal))
                {
                    return await ToAuthenticatedAsync(byEmail, cancellationToken);
                }

                throw new ServiceException(
                    ServiceErrorKind.Conflict,
                    "Аккаунт уже привязан к другой идентичности этого провайдера.");
            }

            var now = clock.GetUtcNow();

            if (byEmail.HasPassword)
            {
                var passwordResetHash = passwordHasher.Hash(
                    Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32)));
                byEmail.RemovePassword(passwordResetHash, now);
            }

            db.ExternalLogins.Add(new ExternalLogin(byEmail.Id, normalizedProvider, normalizedSubject, now));

            if (!byEmail.IsEmailConfirmed)
            {
                byEmail.ConfirmEmail(now);
            }

            byEmail.ApplyExternalDisplayName(displayName, now);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return await ToAuthenticatedAsync(byEmail, cancellationToken);
            }
            catch (DbUpdateException)
            {
                return await ResolveAfterConflictAsync(
                    normalizedProvider,
                    normalizedSubject,
                    normalizedEmail,
                    cancellationToken);
            }
        }

        var placeholderHash = passwordHasher.Hash(
            Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32)));
        var createdAt = clock.GetUtcNow();
        var user = User.CreatePasswordless(normalizedEmail, placeholderHash, createdAt);
        user.ApplyExternalDisplayName(displayName, createdAt);

        db.Users.Add(user);
        db.ExternalLogins.Add(new ExternalLogin(user.Id, normalizedProvider, normalizedSubject, createdAt));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return await ToAuthenticatedAsync(user, cancellationToken);
        }
        catch (DbUpdateException)
        {
            return await ResolveAfterConflictAsync(
                normalizedProvider,
                normalizedSubject,
                normalizedEmail,
                cancellationToken);
        }
    }

    /// <summary>
    /// Привязывает идентичность внешнего провайдера к текущему аккаунту. Email аккаунта
    /// не меняется; совпадающий email подтверждается, потому что провайдер подтвердил владение им.
    /// </summary>
    public async Task<AuthenticatedUser> LinkAsync(
        Guid userId,
        string provider,
        string? subject,
        string? email,
        string? displayName,
        bool emailVerified,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = ExternalLogin.NormalizeProvider(provider);
        var normalizedSubject = ValidateIdentity(subject, email, emailVerified);
        var normalizedEmail = NormalizeEmail(email!);

        var user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.Unauthorized, "Требуется вход.");

        if (user.IsBlocked)
        {
            throw new ServiceException(
                ServiceErrorKind.Forbidden,
                "Аккаунт заблокирован. Обратитесь к администрации.");
        }

        var existing = await db.ExternalLogins.FirstOrDefaultAsync(
            login => login.UserId == userId && login.Provider == normalizedProvider,
            cancellationToken);

        if (existing is not null
            && !string.Equals(existing.Subject, normalizedSubject, StringComparison.Ordinal))
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "К аккаунту уже привязана другая идентичность этого провайдера.");
        }

        var owner = await db.ExternalLogins.FirstOrDefaultAsync(
            login => login.Provider == normalizedProvider
                && login.Subject == normalizedSubject
                && login.UserId != userId,
            cancellationToken);

        if (owner is not null)
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Эта идентичность уже привязана к другому аккаунту.");
        }

        var now = clock.GetUtcNow();

        if (existing is null)
        {
            db.ExternalLogins.Add(new ExternalLogin(userId, normalizedProvider, normalizedSubject, now));
        }

        if (!user.IsEmailConfirmed
            && string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            user.ConfirmEmail(now);
        }

        user.ApplyExternalDisplayName(displayName, now);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Не удалось привязать идентичность: она уже используется.");
        }

        return await ToAuthenticatedAsync(user, cancellationToken);
    }

    private async Task<AuthenticatedUser> ResolveAfterConflictAsync(
        string provider,
        string subject,
        string email,
        CancellationToken cancellationToken)
    {
        var linked = await db.ExternalLogins
            .AsNoTracking()
            .FirstOrDefaultAsync(
                login => login.Provider == provider && login.Subject == subject,
                cancellationToken);

        if (linked is not null)
        {
            var linkedUser = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == linked.UserId, cancellationToken);

            if (linkedUser is not null)
            {
                return await ToAuthenticatedAsync(linkedUser, cancellationToken);
            }
        }

        var byEmail = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (byEmail is not null)
        {
            var match = await db.ExternalLogins
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    login => login.UserId == byEmail.Id
                        && login.Provider == provider
                        && login.Subject == subject,
                    cancellationToken);

            if (match is not null)
            {
                return await ToAuthenticatedAsync(byEmail, cancellationToken);
            }
        }

        throw new ServiceException(
            ServiceErrorKind.Conflict,
            "Не удалось завершить вход. Попробуйте ещё раз.");
    }

    private async Task<AuthenticatedUser> ToAuthenticatedAsync(
        User user,
        CancellationToken cancellationToken)
    {
        if (user.IsBlocked)
        {
            throw new ServiceException(
                ServiceErrorKind.Forbidden,
                "Аккаунт заблокирован. Обратитесь к администрации.");
        }

        var providers = await db.ExternalLogins
            .Where(login => login.UserId == user.Id)
            .Select(login => login.Provider)
            .ToListAsync(cancellationToken);

        return AuthenticatedUser.From(user, providers);
    }

    private static string ValidateIdentity(string? subject, string? email, bool emailVerified)
    {
        var normalizedSubject = subject?.Trim();

        if (string.IsNullOrEmpty(normalizedSubject)
            || normalizedSubject.Length > ExternalLogin.MaxSubjectLength)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Провайдер не вернул корректный идентификатор пользователя.");
        }

        if (!emailVerified)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Провайдер не подтвердил адрес электронной почты.");
        }

        var normalizedEmail = email?.Trim();

        if (string.IsNullOrEmpty(normalizedEmail)
            || normalizedEmail.Length > MaxEmailLength
            || !normalizedEmail.Contains('@'))
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Провайдер не вернул корректный адрес электронной почты.");
        }

        return normalizedSubject;
    }

    /// <summary>Приводит email к единому виду: обрезка пробелов и нижний регистр.</summary>
    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();
}
