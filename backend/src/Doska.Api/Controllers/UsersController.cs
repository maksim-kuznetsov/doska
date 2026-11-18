using Doska.Api.Contracts;
using Doska.Api.Infrastructure;
using Doska.Application.Moderation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doska.Api.Controllers;

/// <summary>Управление доверием и блокировкой пользователей.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthorizationPolicies.ModeratorOrAdmin)]
public sealed class UsersController(TrustService trustService) : ControllerBase
{
    /// <summary>Выдаёт доверие пользователю.</summary>
    [HttpPost("{id:guid}/trust")]
    public async Task<ActionResult<UserResponse>> GrantTrust(Guid id, CancellationToken cancellationToken)
        => Ok(UserResponse.From(await trustService.GrantAsync(id, cancellationToken)));

    /// <summary>Отзывает доверие у пользователя.</summary>
    [HttpDelete("{id:guid}/trust")]
    public async Task<ActionResult<UserResponse>> RevokeTrust(Guid id, CancellationToken cancellationToken)
        => Ok(UserResponse.From(await trustService.RevokeAsync(id, cancellationToken)));

    /// <summary>Блокирует пользователя.</summary>
    [HttpPost("{id:guid}/block")]
    public async Task<ActionResult<UserResponse>> Block(Guid id, CancellationToken cancellationToken)
        => Ok(UserResponse.From(await trustService.BlockAsync(id, cancellationToken)));

    /// <summary>Снимает блокировку пользователя.</summary>
    [HttpPost("{id:guid}/unblock")]
    public async Task<ActionResult<UserResponse>> Unblock(Guid id, CancellationToken cancellationToken)
        => Ok(UserResponse.From(await trustService.UnblockAsync(id, cancellationToken)));
}
