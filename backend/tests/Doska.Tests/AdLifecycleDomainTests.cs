using Doska.Domain.Entities;
using Doska.Domain.Enums;

namespace Doska.Tests;

/// <summary>Правила жизненного цикла объявления.</summary>
public sealed class AdLifecycleDomainTests
{
    [Fact]
    public void Active_ad_can_be_marked_sold_only_once()
    {
        var ad = new Ad { Status = AdStatus.Active };

        ad.MarkSold();
        Assert.Equal(AdStatus.Sold, ad.Status);

        Assert.Throws<InvalidOperationException>(() => ad.MarkSold());
    }

    [Fact]
    public void Trusted_author_returns_sold_ad_to_active_and_untrusted_to_moderation()
    {
        var now = new DateTimeOffset(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);
        var trusted = new Ad { Status = AdStatus.Sold };
        var untrusted = new Ad { Status = AdStatus.Sold };

        trusted.ReturnToSale(true, now);
        untrusted.ReturnToSale(false, now);

        Assert.Equal(AdStatus.Active, trusted.Status);
        Assert.Equal(now, trusted.PublishedAt);
        Assert.Equal(AdStatus.OnModeration, untrusted.Status);
    }

    [Fact]
    public void Only_active_ad_can_be_archived()
    {
        var ad = new Ad { Status = AdStatus.Active };

        ad.Archive();
        Assert.Equal(AdStatus.Archived, ad.Status);

        Assert.Throws<InvalidOperationException>(() => ad.Archive());
    }

    [Fact]
    public void Renew_republishes_archived_ad_and_resets_reminder()
    {
        var now = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
        var ad = new Ad
        {
            Status = AdStatus.Archived,
            PublishedAt = now.AddDays(-10),
            ReminderSentAt = now.AddDays(-2),
        };

        ad.Renew(now);

        Assert.Equal(AdStatus.Active, ad.Status);
        Assert.Equal(now, ad.PublishedAt);
        Assert.Null(ad.ReminderSentAt);
    }

    [Fact]
    public void Sold_ad_cannot_be_renewed()
    {
        var ad = new Ad { Status = AdStatus.Sold };

        Assert.Throws<InvalidOperationException>(() => ad.Renew(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Untrusted_edit_returns_active_ad_to_moderation()
    {
        var ad = new Ad { Status = AdStatus.Active, ModerationReason = "старая причина" };

        ad.CompleteEdit(trustedAuthor: false);

        Assert.Equal(AdStatus.OnModeration, ad.Status);
        Assert.Null(ad.ModerationReason);
    }

    [Fact]
    public void Trusted_edit_keeps_status()
    {
        var ad = new Ad { Status = AdStatus.Active };

        ad.CompleteEdit(trustedAuthor: true);

        Assert.Equal(AdStatus.Active, ad.Status);
    }
}