using Doska.Api.Contracts;
using Doska.Api.Infrastructure;
using Doska.Application.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doska.Api.Controllers;

/// <summary>Управление учётными записями; доступно только администратору.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public sealed class AdminUsersController(UserService userService) : ControllerBase
{
    /// <summary>Список пользователей.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> List(CancellationToken cancellationToken)
    {
        var users = await userService.GetListAsync(cancellationToken);
        return Ok(users.Select(AdminUserResponse.From).ToList());
    }

    /// <summary>Меняет роль пользователя.</summary>
    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<AdminUserResponse>> ChangeRole(
        Guid id,
        [FromBody] ChangeRoleRequest request,
        CancellationToken cancellationToken)
        => Ok(AdminUserResponse.From(await userService.ChangeRoleAsync(id, request.Role, cancellationToken)));
}