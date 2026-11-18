using Doska.Api.Contracts;
using Doska.Api.Infrastructure;
using Doska.Application.Moderation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Doska.Api.Controllers;

/// <summary>Подача и разбор жалоб на объявления.</summary>
[ApiController]
[Route("api/complaints")]
[Authorize]
public sealed class ComplaintsController(ComplaintService complaintService) : ControllerBase
{
    /// <summary>Подаёт жалобу на объявление.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    public async Task<IActionResult> File(
        [FromBody] FileComplaintRequest request,
        CancellationToken cancellationToken)
    {
        await complaintService.FileAsync(
            request.AdId,
            User.GetRequiredUserId(),
            request.Reason,
            cancellationToken);

        return NoContent();
    }

    /// <summary>Очередь открытых жалоб.</summary>
    [HttpGet("queue")]
    [Authorize(Policy = AuthorizationPolicies.ModeratorOrAdmin)]
    public async Task<ActionResult<IReadOnlyList<ComplaintQueueItemResponse>>> Queue(CancellationToken cancellationToken)
    {
        var queue = await complaintService.GetQueueAsync(cancellationToken);
        return Ok(queue.Select(ComplaintQueueItemResponse.From).ToList());
    }

    /// <summary>Закрывает жалобу, оставляя или скрывая объявление.</summary>
    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = AuthorizationPolicies.ModeratorOrAdmin)]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] ResolveComplaintRequest request,
        CancellationToken cancellationToken)
    {
        await complaintService.ResolveAsync(
            id,
            User.GetRequiredUserId(),
            request.HideAd,
            request.HideReason,
            cancellationToken);

        return NoContent();
    }
}
