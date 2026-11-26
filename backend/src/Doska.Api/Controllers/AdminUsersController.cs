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
    /// <summary>Страница пользователей.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultResponse<AdminUserResponse>>> List(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await userService.GetListAsync(
            page ?? 1,
            pageSize ?? UserService.DefaultPageSize,
            cancellationToken);

        return Ok(new PagedResultResponse<AdminUserResponse>(
            result.Items.Select(AdminUserResponse.From).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize));
    }

    /// <summary>Возвращает свойства выбранного пользователя.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminUserResponse>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(AdminUserResponse.From(await userService.GetByIdAsync(id, cancellationToken)));

    /// <summary>Сохраняет полный набор изменяемых свойств пользователя.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminUserResponse>> Update(
        Guid id,
        [FromBody] UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
        => Ok(AdminUserResponse.From(await userService.UpdateAsync(
            id,
            request.Role,
            request.IsTrusted,
            request.IsBlocked,
            request.IsEmailConfirmed,
            request.DisplayName,
            request.Contacts,
            cancellationToken)));
}