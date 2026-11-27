using System.Buffers.Text;
using System.Security.Cryptography;
using Doska.Application.Abstractions;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Auth;

/// <summary>Сведения о вошедшем пользователе, безопасные для передачи клиенту.</summary>
public sealed record AuthenticatedUser(
    Guid Id,
    string Email,
    string Role,
    bool IsTrusted,
    bool IsEmailConfirmed,
    string? DisplayName,
    string? Contacts,
    bool HasPassword,
    bool IsGoogleLinked)
{
    /// <summary>Создаёт сведения о пользователе из сущности.</summary>
    public static AuthenticatedUser From(User user)
        => new(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.IsTrusted,
            user.IsEmailConfirmed,
            user.DisplayName,
            user.Contacts,
            user.HasPassword,
            user.GoogleSubject is not null);
}

/// <summary>Регистрация, вход и подтверждение email.</summary>
public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IPublicUrlProvider publicUrl,
    TimeProvider clock)
{
    /// <summary>Минимальная длина пароля.</summary>
    public const int MinPasswordLength = 8;

    private static readonly TimeSpan ConfirmationTokenLifetime = TimeSpan.FromDays(1);

    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    /// <summary>Регистрирует неподтверждённый аккаунт и отправляет код подтверждения.</summary>
    public async Task<AuthenticatedUser> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail.Length == 0 || !normalizedEmail.Contains('@'))
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Укажите корректный email.");
        }

        ValidatePassword(password);

        var exists = await db.Users.AnyAsync(
            candidate => candidate.Email == normalizedEmail,
            cancellationToken);

        if (exists)
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Не удалось завершить регистрацию. Проверьте данные и попробуйте снова.");
        }

        var user = new User(
            normalizedEmail,
            passwordHasher.Hash(password),
            UserRole.User,
            clock.GetUtcNow());

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Не удалось завершить регистрацию. Проверьте данные и попробуйте снова.");
        }

        var rawToken = CreateToken(user.Id, TokenPurpose.EmailConfirmation, ConfirmationTokenLifetime);
        await db.SaveChangesAsync(cancellationToken);
        await SendConfirmationEmailAsync(user, rawToken, cancellationToken);

        return AuthenticatedUser.From(user);
    }

    /// <summary>Проверяет учётные данные и возвращает пользователя.</summary>
    public async Task<AuthenticatedUser> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Email == normalizedEmail,
            cancellationToken);

        if (user is null || !user.HasPassword || !passwordHasher.Verify(password, user.PasswordHash))
        {
            throw new ServiceException(ServiceErrorKind.Unauthorized, "Неверный email или пароль.");
        }

        if (user.IsBlocked)
        {
            throw new ServiceException(ServiceErrorKind.Forbidden, "Аккаунт заблокирован. Обратитесь к администрации.");
        }

        return AuthenticatedUser.From(user);
    }

    /// <summary>Подтверждает email по одноразовому коду.</summary>
    public async Task ConfirmEmailAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Код подтверждения не указан.");
        }

        var hash = TokenHasher.Hash(rawToken);
        var now = clock.GetUtcNow();
        var token = await db.UserTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.TokenHash == hash && candidate.Purpose == TokenPurpose.EmailConfirmation,
                cancellationToken);

        if (token is null || token.UsedAt is not null || token.ExpiresAt <= now)
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Ссылка подтверждения недействительна или устарела.");
        }

        var user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == token.UserId,
            cancellationToken);

        if (user is null)
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Ссылка подтверждения недействительна или устарела.");
        }

        var consumed = await db.UserTokens
            .Where(candidate =>
                candidate.Id == token.Id
                && candidate.UsedAt == null
                && candidate.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(candidate => candidate.UsedAt, now)
                    .SetProperty(candidate => candidate.UpdatedAt, now),
                cancellationToken);

        if (consumed != 1)
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Ссылка подтверждения недействительна или устарела.");
        }

        user.ConfirmEmail(now);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Повторно отправляет письмо подтверждения. Письмо уходит только для существующего
    /// неподтверждённого аккаунта, но метод всегда завершается успешно, не раскрывая состояние.
    /// </summary>
    public async Task RequestEmailConfirmationResendAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (normalizedEmail.Length == 0)
        {
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Email == normalizedEmail,
            cancellationToken);

        if (user is null || user.IsBlocked || user.IsEmailConfirmed)
        {
            return;
        }

        var now = clock.GetUtcNow();

        var pending = await db.UserTokens
            .Where(token =>
                token.UserId == user.Id
                && token.Purpose == TokenPurpose.EmailConfirmation
                && token.UsedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in pending)
        {
            token.MarkUsed(now);
        }

        var rawToken = CreateToken(user.Id, TokenPurpose.EmailConfirmation, ConfirmationTokenLifetime);
        await db.SaveChangesAsync(cancellationToken);
        await SendConfirmationEmailAsync(user, rawToken, cancellationToken);
    }

    /// <summary>
    /// Запрашивает сброс пароля. Отправляет письмо только для существующего адреса,
    /// но всегда завершается успешно, не раскрывая, зарегистрирован ли адрес.
    /// </summary>
    public async Task RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (normalizedEmail.Length == 0)
        {
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Email == normalizedEmail,
            cancellationToken);

        if (user is null || user.IsBlocked)
        {
            return;
        }

        var now = clock.GetUtcNow();

        var pending = await db.UserTokens
            .Where(token =>
                token.UserId == user.Id
                && token.Purpose == TokenPurpose.PasswordReset
                && token.UsedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in pending)
        {
            token.MarkUsed(now);
        }

        var rawToken = CreateToken(user.Id, TokenPurpose.PasswordReset, ResetTokenLifetime);
        await db.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            user.Email,
            "Сброс пароля",
            $"Чтобы задать новый пароль, откройте ссылку {BuildLink("/reset-password", rawToken)}",
            cancellationToken);
    }

    /// <summary>Меняет пароль по одноразовой ссылке и делает её недействительной.</summary>
    public async Task ResetPasswordAsync(
        string rawToken,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Ссылка сброса недействительна или устарела. Запросите новую.");
        }

        ValidatePassword(password);

        var hash = TokenHasher.Hash(rawToken);
        var now = clock.GetUtcNow();
        var token = await db.UserTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.TokenHash == hash && candidate.Purpose == TokenPurpose.PasswordReset,
                cancellationToken);

        if (token is null || token.UsedAt is not null || token.ExpiresAt <= now)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Ссылка сброса недействительна или устарела. Запросите новую.");
        }

        var user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == token.UserId,
            cancellationToken);

        if (user is null)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Ссылка сброса недействительна или устарела. Запросите новую.");
        }

        var consumed = await db.UserTokens
            .Where(candidate =>
                candidate.Id == token.Id
                && candidate.UsedAt == null
                && candidate.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(candidate => candidate.UsedAt, now)
                    .SetProperty(candidate => candidate.UpdatedAt, now),
                cancellationToken);

        if (consumed != 1)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                "Ссылка сброса недействительна или устарела. Запросите новую.");
        }

        user.SetPassword(passwordHasher.Hash(password), now);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Возвращает сведения о пользователе по идентификатору.</summary>
    public async Task<AuthenticatedUser> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.Unauthorized, "Требуется вход.");

        if (user.IsBlocked)
        {
            throw new ServiceException(ServiceErrorKind.Forbidden, "Аккаунт заблокирован. Обратитесь к администрации.");
        }

        return AuthenticatedUser.From(user);
    }

    private string CreateToken(Guid userId, TokenPurpose purpose, TimeSpan lifetime)
    {
        var rawToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var now = clock.GetUtcNow();
        db.UserTokens.Add(new UserToken(
            userId,
            TokenHasher.Hash(rawToken),
            purpose,
            now.Add(lifetime),
            now));

        return rawToken;
    }

    private string BuildLink(string path, string rawToken)
    {
        return $"{publicUrl.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}?token={Uri.EscapeDataString(rawToken)}";
    }

    private Task SendConfirmationEmailAsync(
        User user,
        string rawToken,
        CancellationToken cancellationToken)
    {
        return emailSender.SendAsync(
            user.Email,
            "Подтверждение email",
            $"Подтвердите email, открыв ссылку {BuildLink("/confirm-email", rawToken)}",
            cancellationToken);
    }

    /// <summary>Проверяет пароль по тем же правилам, что и регистрация.</summary>
    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
        {
            throw new ServiceException(
                ServiceErrorKind.Validation,
                $"Пароль должен содержать не менее {MinPasswordLength} символов.");
        }

        if (!password.Any(char.IsLetter))
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Пароль не должен состоять только из цифр.");
        }
    }

    /// <summary>Приводит email к единому виду: обрезка пробелов и нижний регистр.</summary>
    private static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();
}
