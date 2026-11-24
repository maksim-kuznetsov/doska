using Doska.Domain.Entities;
using Doska.Domain.Enums;

namespace Doska.Tests;

/// <summary>Правила жизненного цикла объявления.</summary>
public sealed class AdLifecycleDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    private static Ad CreateActiveAd(DateTimeOffset? publishedAt = null)
        => new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Заголовок",
            "Описание",
            100m,
            "Контакты",
            trustedAuthor: true,
            publishedAt ?? Now);

    [Fact]
    public void Active_ad_can_be_marked_sold_only_once()
    {
        var ad = CreateActiveAd();

        ad.MarkSold(Now);
        Assert.Equal(AdStatus.Sold, ad.Status);

        Assert.Throws<InvalidOperationException>(() => ad.MarkSold(Now));
    }

    [Fact]
    public void Trusted_author_returns_sold_ad_to_active_and_untrusted_to_moderation()
    {
        var trusted = CreateActiveAd();
        trusted.MarkSold(Now);
        var untrusted = CreateActiveAd();
        untrusted.MarkSold(Now);

        trusted.ReturnToSale(true, Now);
        untrusted.ReturnToSale(false, Now);

        Assert.Equal(AdStatus.Active, trusted.Status);
        Assert.Equal(Now, trusted.PublishedAt);
        Assert.Equal(AdStatus.OnModeration, untrusted.Status);
    }

    [Fact]
    public void Only_active_ad_can_be_archived()
    {
        var ad = CreateActiveAd();

        ad.Archive(Now);
        Assert.Equal(AdStatus.Archived, ad.Status);

        Assert.Throws<InvalidOperationException>(() => ad.Archive(Now));
    }

    [Fact]
    public void Renew_republishes_archived_ad_and_resets_reminder()
    {
        var ad = CreateActiveAd(Now.AddDays(-10));
        ad.MarkReminderSent(Now.AddDays(-2));
        ad.Archive(Now);

        ad.Renew(Now);

        Assert.Equal(AdStatus.Active, ad.Status);
        Assert.Equal(Now, ad.PublishedAt);
        Assert.Null(ad.ReminderSentAt);
    }

    [Fact]
    public void Sold_ad_cannot_be_renewed()
    {
        var ad = CreateActiveAd();
        ad.MarkSold(Now);

        Assert.Throws<InvalidOperationException>(() => ad.Renew(Now));
    }

    [Fact]
    public void Untrusted_edit_returns_active_ad_to_moderation()
    {
        var ad = CreateActiveAd();

        ad.CompleteEdit(trustedAuthor: false, Now);

        Assert.Equal(AdStatus.OnModeration, ad.Status);
        Assert.Null(ad.ModerationReason);
    }

    [Fact]
    public void Trusted_edit_keeps_status()
    {
        var ad = CreateActiveAd();

        ad.CompleteEdit(trustedAuthor: true, Now);

        Assert.Equal(AdStatus.Active, ad.Status);
    }
}
