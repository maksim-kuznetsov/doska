using Doska.Domain.Entities;
using Doska.Domain.Enums;

namespace Doska.Tests;

/// <summary>Правила переходов статусов объявления, жалоб и доверия.</summary>
public sealed class ModerationDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);

    private static Ad CreateAd(bool trustedAuthor)
        => new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Заголовок",
            "Описание",
            100m,
            "Контакты",
            trustedAuthor,
            Now);

    private static User CreateUser()
        => new("user@example.com", "hash", UserRole.User, Now);

    [Fact]
    public void Approve_moves_ad_to_active_and_sets_published_at()
    {
        var ad = CreateAd(trustedAuthor: false);

        ad.Approve(Now);

        Assert.Equal(AdStatus.Active, ad.Status);
        Assert.Equal(Now, ad.PublishedAt);
        Assert.Null(ad.ModerationReason);
    }

    [Fact]
    public void Approve_rejects_ad_not_on_moderation()
    {
        var ad = CreateAd(trustedAuthor: true);

        Assert.Throws<InvalidOperationException>(() => ad.Approve(Now));
    }

    [Fact]
    public void Reject_requires_reason_and_sets_status()
    {
        var ad = CreateAd(trustedAuthor: false);

        Assert.Throws<ArgumentException>(() => ad.Reject("   ", Now));

        ad.Reject(" Нарушение правил ", Now);

        Assert.Equal(AdStatus.Rejected, ad.Status);
        Assert.Equal("Нарушение правил", ad.ModerationReason);
    }

    [Fact]
    public void Hide_requires_active_ad_and_reason()
    {
        var onModeration = CreateAd(trustedAuthor: false);
        Assert.Throws<InvalidOperationException>(() => onModeration.Hide("причина", Now));

        var active = CreateAd(trustedAuthor: true);
        Assert.Throws<ArgumentException>(() => active.Hide(string.Empty, Now));

        active.Hide("Нарушение правил", Now);

        Assert.Equal(AdStatus.Hidden, active.Status);
        Assert.Equal("Нарушение правил", active.ModerationReason);
    }

    [Fact]
    public void Closing_complaint_records_moderator_and_time()
    {
        var complaint = new AdComplaint(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "причина",
            Now);
        var moderatorId = Guid.CreateVersion7();
        var closedAt = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

        complaint.Close(moderatorId, closedAt);

        Assert.Equal(ComplaintStatus.Closed, complaint.Status);
        Assert.Equal(moderatorId, complaint.ClosedByUserId);
        Assert.Equal(closedAt, complaint.ClosedAt);

        Assert.Throws<InvalidOperationException>(() => complaint.Close(Guid.CreateVersion7(), closedAt));
    }

    [Fact]
    public void Three_approvals_grant_trust_and_counter_keeps_growing()
    {
        var user = CreateUser();

        Assert.False(user.RegisterApprovedAd(Now));
        Assert.False(user.RegisterApprovedAd(Now));
        Assert.True(user.RegisterApprovedAd(Now));

        Assert.True(user.IsTrusted);
        Assert.Equal(3, user.ApprovedAdsCount);

        Assert.False(user.RegisterApprovedAd(Now));
        Assert.Equal(4, user.ApprovedAdsCount);
    }

    [Fact]
    public void Trust_can_be_granted_and_revoked_manually()
    {
        var user = CreateUser();

        user.GrantTrust(Now);
        Assert.True(user.IsTrusted);

        user.RegisterApprovedAd(Now);
        Assert.Equal(1, user.ApprovedAdsCount);

        user.RevokeTrust(Now);
        Assert.False(user.IsTrusted);
        Assert.Equal(0, user.ApprovedAdsCount);
    }

    [Fact]
    public void Blocking_sets_and_clears_blocked_at()
    {
        var user = CreateUser();
        var blockedAt = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

        user.Block(blockedAt);
        Assert.True(user.IsBlocked);
        Assert.Equal(blockedAt, user.BlockedAt);

        user.Unblock(Now);
        Assert.False(user.IsBlocked);
    }

    [Fact]
    public void Email_confirmation_can_be_set_both_ways()
    {
        var user = CreateUser();
        Assert.False(user.IsEmailConfirmed);

        user.ConfirmEmail(Now);
        Assert.True(user.IsEmailConfirmed);

        user.SetEmailConfirmed(false, Now);
        Assert.False(user.IsEmailConfirmed);

        user.SetEmailConfirmed(true, Now);
        Assert.True(user.IsEmailConfirmed);
    }
}
