using Doska.Application.Accounts;

namespace Doska.Api.Contracts;

/// <summary>Запрос смены роли пользователя.</summary>
public sealed record ChangeRoleRequest(string Role);

/// <summary>Сведения о пользователе для администратора.</summary>
public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    string Role,
    bool IsTrusted,
    bool IsBlocked,
    bool IsEmailConfirmed)
{
    /// <summary>Создаёт ответ из представления.</summary>
    public static AdminUserResponse From(AdminUserView user)
        => new(user.Id, user.Email, user.Role, user.IsTrusted, user.IsBlocked, user.IsEmailConfirmed);
}