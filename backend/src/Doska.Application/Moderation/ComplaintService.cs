using Doska.Application.Abstractions;
using Doska.Application.Common;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Doska.Application.Moderation;

/// <summary>Элемент очереди жалоб.</summary>
public sealed record ComplaintQueueItem(
    Guid Id,
    Guid AdId,
    string AdTitle,
    string AuthorEmail,
    string Reason,
    DateTimeOffset CreatedAt);

/// <summary>Подача, разбор и закрытие жалоб на объявления.</summary>
public sealed class ComplaintService(IAppDbContext db, IEmailSender email, TimeProvider clock)
{
    /// <summary>
    /// Подаёт жалобу на чужое активное объявление. Одна жалоба на объявление от пользователя.
    /// </summary>
    public async Task FileAsync(
        Guid adId,
        Guid authorId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(candidate => candidate.Id == authorId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.Unauthorized, "Требуется вход.");

        if (user.IsBlocked)
        {
            throw new ServiceException(ServiceErrorKind.Forbidden, "Аккаунт заблокирован.");
        }

        if (!user.IsEmailConfirmed)
        {
            throw new ServiceException(ServiceErrorKind.Forbidden, "Подтвердите email, чтобы пожаловаться на объявление.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Укажите причину жалобы.");
        }

        var ad = await db.Ads.FirstOrDefaultAsync(candidate => candidate.Id == adId, cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.NotFound, "Объявление не найдено.");

        if (ad.Status != AdStatus.Active)
        {
            throw new ServiceException(ServiceErrorKind.Validation, "Жаловаться можно только на активное объявление.");
        }

        if (ad.AuthorId == authorId)
        {
            throw new ServiceException(ServiceErrorKind.Forbidden, "Нельзя жаловаться на собственное объявление.");
        }

        var alreadyFiled = await db.AdComplaints.AnyAsync(
            complaint => complaint.AdId == adId && complaint.AuthorId == authorId,
            cancellationToken);

        if (alreadyFiled)
        {
            throw new ServiceException(ServiceErrorKind.Conflict, "Жалоба на это объявление уже отправлена.");
        }

        db.AdComplaints.Add(new AdComplaint
        {
            AdId = adId,
            AuthorId = authorId,
            Reason = reason.Trim(),
            Status = ComplaintStatus.Open,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Очередь открытых жалоб: от самых старых к новым.</summary>
    public async Task<IReadOnlyList<ComplaintQueueItem>> GetQueueAsync(
        CancellationToken cancellationToken = default)
        => await db.AdComplaints
            .Where(complaint => complaint.Status == ComplaintStatus.Open)
            .OrderBy(complaint => complaint.CreatedAt)
            .Select(complaint => new ComplaintQueueItem(
                complaint.Id,
                complaint.AdId,
                complaint.Ad!.Title,
                complaint.Author!.Email,
                complaint.Reason,
                complaint.CreatedAt))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Закрывает жалобу, оставляя объявление или скрывая его с причиной.
    /// </summary>
    public async Task ResolveAsync(
        Guid complaintId,
        Guid moderatorId,
        bool hideAd,
        string? hideReason,
        CancellationToken cancellationToken = default)
    {
        var complaint = await db.AdComplaints.FirstOrDefaultAsync(
            candidate => candidate.Id == complaintId,
            cancellationToken)
            ?? throw new ServiceException(ServiceErrorKind.NotFound, "Жалоба не найдена.");

        if (complaint.Status != ComplaintStatus.Open)
        {
            throw new ServiceException(ServiceErrorKind.Conflict, "Жалоба уже закрыта.");
        }

        if (hideAd)
        {
            var ad = await db.Ads.FirstOrDefaultAsync(candidate => candidate.Id == complaint.AdId, cancellationToken)
                ?? throw new ServiceException(ServiceErrorKind.NotFound, "Объявление не найдено.");

            ad.Hide(hideReason ?? string.Empty);
            await db.SaveChangesAsync(cancellationToken);

            var author = await db.Users.FirstAsync(user => user.Id == ad.AuthorId, cancellationToken);
            await email.SendAsync(
                author.Email,
                "Объявление скрыто",
                $"Объявление «{ad.Title}» скрыто по жалобе. Причина: {ad.ModerationReason}",
                cancellationToken);
        }

        complaint.Close(moderatorId, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
