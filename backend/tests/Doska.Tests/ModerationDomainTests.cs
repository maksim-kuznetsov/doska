using Doska.Domain.Entities;
using Doska.Domain.Enums;

namespace Doska.Tests;

/// <summary>Правила переходов статусов объявления, жалоб и доверия.</summary>
public sealed class ModerationDomainTests
{
    [Fact]
    public void Approve_moves_ad_to_active_and_sets_published_at()
    {
        var ad = new Ad { Status = AdStatus.OnModeration };
        var now = new DateTimeOffset(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

        ad.Approve(now);

        Assert.Equal(AdStatus.Active, ad.Status);
        Assert.Equal(now, ad.PublishedAt);
        Assert.Null(ad.ModerationReason);
    }

    [Fact]
    public void Approve_rejects_ad_not_on_moderation()
    {
        var ad = new Ad { Status = AdStatus.Active };

        Assert.Throws<InvalidOperationException>(() => ad.Approve(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Reject_requires_reason_and_sets_status()
    {
        var ad = new Ad { Status = AdStatus.OnModeration };

        Assert.Throws<ArgumentException>(() => ad.Reject("   "));

        ad.Reject(" Нарушение правил ");

        Assert.Equal(AdStatus.Rejected, ad.Status);
        Assert.Equal("Нарушение правил", ad.ModerationReason);
    }

    [Fact]
    public void Hide_requires_active_ad_and_reason()
    {
        var onModeration = new Ad { Status = AdStatus.OnModeration };
        Assert.Throws<InvalidOperationException>(() => onModeration.Hide("причина"));

        var active = new Ad { Status = AdStatus.Active };
        Assert.Throws<ArgumentException>(() => active.Hide(string.Empty));

        active.Hide("Нарушение правил");

        Assert.Equal(AdStatus.Hidden, active.Status);
        Assert.Equal("Нарушение правил", active.ModerationReason);
    }

    [Fact]
    public void Closing_complaint_records_moderator_and_time()
    {
        var complaint = new AdComplaint { Status = ComplaintStatus.Open };
        var moderatorId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

        complaint.Close(moderatorId, now);

        Assert.Equal(ComplaintStatus.Closed, complaint.Status);
        Assert.Equal(moderatorId, complaint.ClosedByUserId);
        Assert.Equal(now, complaint.ClosedAt);

        Assert.Throws<InvalidOperationException>(() => complaint.Close(Guid.CreateVersion7(), now));
    }

    [Fact]
    public void Three_approvals_grant_trust_and_counter_keeps_growing()
    {
        var user = new User();

        Assert.False(user.RegisterApprovedAd());
        Assert.False(user.RegisterApprovedAd());
        Assert.True(user.RegisterApprovedAd());

        Assert.True(user.IsTrusted);
        Assert.Equal(3, user.ApprovedAdsCount);

        Assert.False(user.RegisterApprovedAd());
        Assert.Equal(4, user.ApprovedAdsCount);
    }

    [Fact]
    public void Trust_can_be_granted_and_revoked_manually()
    {
        var user = new User();

        user.GrantTrust();
        Assert.True(user.IsTrusted);

        user.RegisterApprovedAd();
        Assert.Equal(1, user.ApprovedAdsCount);

        user.RevokeTrust();
        Assert.False(user.IsTrusted);
        Assert.Equal(0, user.ApprovedAdsCount);
    }

    [Fact]
    public void Blocking_sets_and_clears_blocked_at()
    {
        var user = new User();
        var now = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

        user.Block(now);
        Assert.True(user.IsBlocked);
        Assert.Equal(now, user.BlockedAt);

        user.Unblock();
        Assert.False(user.IsBlocked);
    }
}
