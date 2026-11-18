using Doska.Api.Contracts;
using Doska.Api.Infrastructure;
using Doska.Application.Moderation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doska.Api.Controllers;

/// <summary>Очередь проверки и решения модератора по объявлениям.</summary>
[ApiController]
[Route("api/moderation")]
[Authorize(Policy = AuthorizationPolicies.ModeratorOrAdmin)]
public sealed class ModerationController(ModerationService moderationService) : ControllerBase
{
    /// <summary>Очередь объявлений на проверке.</summary>
    [HttpGet("queue")]
    public async Task<ActionResult<IReadOnlyList<ModerationQueueItemResponse>>> Queue(CancellationToken cancellationToken)
    {
        var queue = await moderationService.GetQueueAsync(cancellationToken);
        return Ok(queue.Select(ModerationQueueItemResponse.From).ToList());
    }

    /// <summary>Одобряет объявление.</summary>
    [HttpPost("ads/{id:guid}/approve")]
    public async Task<ActionResult<ModerationDecisionResponse>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var decision = await moderationService.ApproveAsync(id, cancellationToken);
        return Ok(ModerationDecisionResponse.From(decision));
    }

    /// <summary>Отклоняет объявление с причиной.</summary>
    [HttpPost("ads/{id:guid}/reject")]
    public async Task<ActionResult<ModerationDecisionResponse>> Reject(
        Guid id,
        [FromBody] ReasonRequest request,
        CancellationToken cancellationToken)
    {
        var decision = await moderationService.RejectAsync(id, request.Reason, cancellationToken);
        return Ok(ModerationDecisionResponse.From(decision));
    }

    /// <summary>Скрывает активное объявление с причиной.</summary>
    [HttpPost("ads/{id:guid}/hide")]
    public async Task<ActionResult<ModerationDecisionResponse>> Hide(
        Guid id,
        [FromBody] ReasonRequest request,
        CancellationToken cancellationToken)
    {
        var decision = await moderationService.HideAsync(id, request.Reason, cancellationToken);
        return Ok(ModerationDecisionResponse.From(decision));
    }
}
