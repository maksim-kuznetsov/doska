using Doska.Application.Moderation;

namespace Doska.Api.Contracts;

/// <summary>Запрос решения с причиной (отклонение, скрытие).</summary>
public sealed record ReasonRequest(string Reason);

/// <summary>Элемент очереди проверки.</summary>
public sealed record ModerationQueueItemResponse(
    Guid Id,
    string Title,
    string AuthorEmail,
    DateTimeOffset SubmittedAt)
{
    /// <summary>Создаёт ответ из элемента очереди.</summary>
    public static ModerationQueueItemResponse From(ModerationQueueItem item)
        => new(item.Id, item.Title, item.AuthorEmail, item.SubmittedAt);
}

/// <summary>Ответ о решении модератора по объявлению.</summary>
public sealed record ModerationDecisionResponse(Guid AdId, string Status, bool TrustGranted)
{
    /// <summary>Создаёт ответ из результата решения.</summary>
    public static ModerationDecisionResponse From(ModerationDecision decision)
        => new(decision.AdId, decision.Status, decision.TrustGranted);
}

/// <summary>Запрос жалобы на объявление.</summary>
public sealed record FileComplaintRequest(Guid AdId, string Reason);

/// <summary>Элемент очереди жалоб.</summary>
public sealed record ComplaintQueueItemResponse(
    Guid Id,
    Guid AdId,
    string AdTitle,
    string AuthorEmail,
    string Reason,
    DateTimeOffset CreatedAt)
{
    /// <summary>Создаёт ответ из элемента очереди жалоб.</summary>
    public static ComplaintQueueItemResponse From(ComplaintQueueItem item)
        => new(item.Id, item.AdId, item.AdTitle, item.AuthorEmail, item.Reason, item.CreatedAt);
}

/// <summary>Запрос решения по жалобе.</summary>
public sealed record ResolveComplaintRequest(bool HideAd, string? HideReason);
