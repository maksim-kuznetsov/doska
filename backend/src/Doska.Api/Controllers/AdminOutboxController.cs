using Doska.Api.Contracts;
using Doska.Api.Infrastructure;
using Doska.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Doska.Api.Controllers;

/// <summary>Чтение писем файлового ящика; доступно только администратору.</summary>
[ApiController]
[Route("api/admin/outbox")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public sealed class AdminOutboxController(IEmailOutbox outbox) : ControllerBase
{
    /// <summary>Список писем файлового ящика от новых к старым.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OutboxMessageResponse>>> List(CancellationToken cancellationToken)
    {
        var messages = await outbox.ListAsync(cancellationToken);
        return Ok(messages.Select(OutboxMessageResponse.From).ToList());
    }

    /// <summary>Содержимое выбранного письма.</summary>
    [HttpGet("{*fileName}")]
    public async Task<ActionResult<OutboxMessageContentResponse>> Read(
        string fileName,
        CancellationToken cancellationToken)
    {
        var result = await outbox.ReadAsync(fileName, cancellationToken);

        return result.Status switch
        {
            EmailOutboxReadStatus.Ok => Ok(OutboxMessageContentResponse.From(result.Text)),
            EmailOutboxReadStatus.InvalidName => BadRequest(),
            _ => NotFound(),
        };
    }
}
