using Doska.Application.Abstractions;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Moderation;

/// <summary>Элемент очереди проверки.</summary>
public sealed record ModerationQueueItem(
    Guid Id,
    string Title,
    string AuthorEmail,
    DateTimeOffset SubmittedAt);

/// <summary>Результат решения модератора по объявлению.</summary>
public sealed record ModerationDecision(Guid AdId, string Status, bool TrustGranted);

/// <summary>Очередь проверки и решения модератора по объявлениям.</summary>
public sealed class ModerationService(IAppDbContext db, IEmailSender email, TimeProvider clock)
{
    /// <summary>Очередь объявлений на проверке: от самых старых к новым.</summary>
    public async Task<IReadOnlyList<ModerationQueueItem>> GetQueueAsync(
        CancellationToken cancellationToken = default)
        => await db.Ads
            .Where(ad => ad.Status == AdStatus.OnModeration)
            .OrderBy(ad => ad.CreatedAt)
            .Select(ad => new ModerationQueueItem(ad.Id, ad.Title, ad.Author!.Email, ad.CreatedAt))
            .ToListAsync(cancellationToken);

    /// <summary>Одобряет объявление и учитывает одобрение для доверия автора.</summary>
    public async Task<ModerationDecision> ApproveAsync(
        Guid adId,
        CancellationToken cancellationToken = default)
    {
        var ad = await LoadAdAsync(adId, cancellationToken);

        ad.Approve(clock.GetUtcNow());

        var author = await db.Users.FirstAsync(user => user.Id == ad.AuthorId, cancellationToken);
        var trustGranted = !author.IsTrusted && author.RegisterApprovedAd(clock.GetUtcNow());

        await db.SaveChangesAsync(cancellationToken);

        return new ModerationDecision(ad.Id, ad.Status.ToString(), trustGranted);
    }

    /// <summary>Отклоняет объявление с обязательной причиной и уведомляет автора письмом.</summary>
    public async Task<ModerationDecision> RejectAsync(
        Guid adId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var ad = await LoadAdAsync(adId, cancellationToken);

        ad.Reject(reason, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        var author = await db.Users.FirstAsync(user => user.Id == ad.AuthorId, cancellationToken);
        await email.SendAsync(
            author.Email,
            "Объявление отклонено",
            $"Объявление «{ad.Title}» отклонено. Причина: {ad.ModerationReason}",
            cancellationToken);

        return new ModerationDecision(ad.Id, ad.Status.ToString(), false);
    }

    /// <summary>Скрывает активное объявление с обязательной причиной и уведомляет автора письмом.</summary>
    public async Task<ModerationDecision> HideAsync(
        Guid adId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var ad = await LoadAdAsync(adId, cancellationToken);

        ad.Hide(reason, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        var author = await db.Users.FirstAsync(user => user.Id == ad.AuthorId, cancellationToken);
        await email.SendAsync(
            author.Email,
            "Объявление скрыто",
            $"Объявление «{ad.Title}» скрыто. Причина: {ad.ModerationReason}",
            cancellationToken);

        return new ModerationDecision(ad.Id, ad.Status.ToString(), false);
    }

    private async Task<Ad> LoadAdAsync(Guid adId, CancellationToken cancellationToken)
        => await db.Ads.FirstOrDefaultAsync(ad => ad.Id == adId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.NotFound, "Объявление не найдено.");
}
