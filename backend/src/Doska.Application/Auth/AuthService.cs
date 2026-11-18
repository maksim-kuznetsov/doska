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
    bool IsEmailConfirmed)
{
    /// <summary>Создаёт сведения о пользователе из сущности.</summary>
    public static AuthenticatedUser From(User user)
        => new(user.Id, user.Email, user.Role.ToString(), user.IsTrusted, user.IsEmailConfirmed);
}

/// <summary>Регистрация, вход и подтверждение email.</summary>
public sealed class AuthService(IAppDbContext db, IPasswordHasher passwordHasher, IEmailSender emailSender, TimeProvider clock)
{
    /// <summary>Минимальная длина пароля.</summary>
    public const int MinPasswordLength = 8;

    private static readonly TimeSpan ConfirmationTokenLifetime = TimeSpan.FromDays(1);

    /// <summary>Регистрирует неподтверждённый аккаунт и отправляет код подтверждения.</summary>
    public async Task<AuthenticatedUser> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();
        if (normalizedEmail.Length == 0 || !normalizedEmail.Contains('@'))
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Укажите корректный email.");
        }

        ValidatePassword(password);

        var exists = await db.Users.AnyAsync(
            user => user.Email.ToLower() == normalizedEmail.ToLower(),
            cancellationToken);

        if (exists)
        {
            throw new ServiceException(
                ServiceErrorKind.Conflict,
                "Не удалось завершить регистрацию. Проверьте данные и попробуйте снова.");
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(password),
            Role = UserRole.User,
            IsEmailConfirmed = false,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var rawToken = CreateToken(user.Id, TokenPurpose.EmailConfirmation);
        await db.SaveChangesAsync(cancellationToken);
        await emailSender.SendAsync(
            user.Email,
            "Подтверждение email",
            $"Подтвердите email, открыв ссылку /confirm-email?token={rawToken}",
            cancellationToken);

        return AuthenticatedUser.From(user);
    }

    /// <summary>Проверяет учётные данные и возвращает пользователя.</summary>
    public async Task<AuthenticatedUser> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim();
        var user = await db.Users.FirstOrDefaultAsync(
            candidate => candidate.Email.ToLower() == normalizedEmail.ToLower(),
            cancellationToken);

        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
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
        var token = await db.UserTokens
            .Include(candidate => candidate.User)
            .FirstOrDefaultAsync(
                candidate => candidate.TokenHash == hash && candidate.Purpose == TokenPurpose.EmailConfirmation,
                cancellationToken);

        if (token is null || token.UsedAt is not null || token.ExpiresAt <= clock.GetUtcNow())
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Ссылка подтверждения недействительна или устарела.");
        }

        if (token.User is not null)
        {
            token.User.IsEmailConfirmed = true;
        }

        token.UsedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Возвращает сведения о пользователе по идентификатору.</summary>
    public async Task<AuthenticatedUser> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.Unauthorized, "Требуется вход.");

        return AuthenticatedUser.From(user);
    }

    private string CreateToken(Guid userId, TokenPurpose purpose)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        db.UserTokens.Add(new UserToken
        {
            UserId = userId,
            TokenHash = TokenHasher.Hash(rawToken),
            Purpose = purpose,
            ExpiresAt = clock.GetUtcNow().Add(ConfirmationTokenLifetime),
        });

        return rawToken;
    }

    private static void ValidatePassword(string password)
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
}
