using System.Buffers.Text;
using System.Security.Cryptography;
using Doska.Application.Abstractions;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Auth;

/// <summary>
/// Разрешение Google-идентичности: вход, автоматическая привязка по email
/// и явная привязка из профиля.
/// </summary>
public sealed class GoogleAuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    TimeProvider clock)
{
    private const int MaxSubjectLength = 255;
    private const int MaxEmailLength = 320;

    /// <summary>
    /// Входит по Google-идентичности или создаёт подтверждённый аккаунт без пароля.
    /// Сначала используется сохранённая привязка, затем email и только потом создаётся аккаунт.
    /// </summary>
    public async Task<AuthenticatedUser> SignInAsync(
        string? googleSubject,
        string? email,
        string? displayName,
        bool emailVerified,
        CancellationToken cancellationToken = default)
    {
        var subject = ValidateIdentity(googleSubject, email, emailVerified);
        var normalizedEmail = NormalizeEmail(email!);

        var linked = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.GoogleSubject == subject,
            cancellationToken);

        if (linked is not null)
        {
            return EnsureNotBlocked(linked);
        }

        var byEmail = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Email == normalizedEmail,
            cancellationToken);

        if (byEmail is not null)
        {
            EnsureNotBlocked(byEmail);

            if (byEmail.GoogleSubject is not null)
            {
                if (string.Equals(byEmail.GoogleSubject, subject, StringComparison.Ordinal))
                {
                    return AuthenticatedUser.From(byEmail);
                }

                throw new ServiceException(
                    ServiceErrorKind.Conflict,
                    "Аккаунт уже привязан к другой Google-идентичности.");
            }

            var now = clock.GetUtcNow();

            if (byEmail.HasPassword)
            {
                var passwordResetHash = passwordHasher.Hash(
                    Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32)));
                byEmail.RemovePassword(passwordResetHash, now);
            }

            byEmail.LinkGoogle(subject, now);

            if (!byEmail.IsEmailConfirmed)
            {
                byEmail.ConfirmEmail(now);
            }

            byEmail.ApplyGoogleDisplayName(displayName, now);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return AuthenticatedUser.From(byEmail);
            }
            catch (DbUpdateException)
            {
                return await ResolveAfterConflictAsync(subject, normalizedEmail, cancellationToken);
            }
        }

        var placeholderHash = passwordHasher.Hash(
            Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32)));
        var createdAt = clock.GetUtcNow();
        var user = User.CreateWithGoogle(normalizedEmail, subject, placeholderHash, createdAt);
        user.ApplyGoogleDisplayName(displayName, createdAt);

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return AuthenticatedUser.From(user);
        }
        catch (DbUpdateException)
        {
            return await ResolveAfterConflictAsync(subject, normalizedEmail, cancellationToken);
        }
    }

    /// <summary>
    /// Привязывает Google-идентичность к текущему аккаунту. Email аккаунта не меняется;
    /// совпадающий email подтверждается, потому что Google подтвердил владение им.
    /// </summary>
    public async Task<AuthenticatedUser> LinkAsync(
        Guid userId,
        string? googleSubject,
        string? email,
        string? displayName,
        bool emailVerified,
        CancellationToken cancellationToken = default)
    {
        var subject = ValidateIdentity(googleSubject, email, emailVerified);
        var normalizedEmail = NormalizeEmail(email!);

        var user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == userId,
            cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.Unauthorized, "Требуется вход.");

        EnsureNotBlocked(user);

        if (user.GoogleSubject is not null
            && !string.Equals(user.GoogleSubject, subject, StringComparison.Ordinal))
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "К аккаунту уже привязана другая Google-идентичность.");
        }

        var owner = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.GoogleSubject == subject && candidate.Id != userId,
            cancellationToken);

        if (owner is not null)
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Эта Google-идентичность уже привязана к другому аккаунту.");
        }

        var now = clock.GetUtcNow();
        user.LinkGoogle(subject, now);

        if (!user.IsEmailConfirmed
            && string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            user.ConfirmEmail(now);
        }

        user.ApplyGoogleDisplayName(displayName, now);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Не удалось привязать Google: идентичность уже используется.");
        }

        return AuthenticatedUser.From(user);
    }

    private async Task<AuthenticatedUser> ResolveAfterConflictAsync(
        string subject,
        string email,
        CancellationToken cancellationToken)
    {
        var linked = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.GoogleSubject == subject, cancellationToken);

        if (linked is not null)
        {
            return EnsureNotBlocked(linked);
        }

        var byEmail = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Email == email,
                cancellationToken);

        if (byEmail is not null
            && string.Equals(byEmail.GoogleSubject, subject, StringComparison.Ordinal))
        {
            return EnsureNotBlocked(byEmail);
        }

        throw new ServiceException(
            ServiceErrorKind.Conflict,
            "Не удалось завершить вход через Google. Попробуйте ещё раз.");
    }

    private static AuthenticatedUser EnsureNotBlocked(User user)
    {
        if (user.IsBlocked)
        {
            throw new ServiceException(
                ServiceErrorKind.Forbidden,
                "Аккаунт заблокирован. Обратитесь к администрации.");
        }

        return AuthenticatedUser.From(user);
    }

    private static string ValidateIdentity(string? googleSubject, string? email, bool emailVerified)
    {
        var subject = googleSubject?.Trim();

        if (string.IsNullOrEmpty(subject) || subject.Length > MaxSubjectLength)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Google не вернул корректный идентификатор пользователя.");
        }

        var normalizedEmail = email?.Trim();

        if (!emailVerified)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Google не подтвердил адрес электронной почты.");
        }

        if (string.IsNullOrEmpty(normalizedEmail)
            || normalizedEmail.Length > MaxEmailLength
            || !normalizedEmail.Contains('@'))
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Google не вернул корректный адрес электронной почты.");
        }

        return subject;
    }

    /// <summary>Приводит email к единому виду: обрезка пробелов и нижний регистр.</summary>
    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();
}