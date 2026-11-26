using Doska.Domain.Entities;
using Doska.Domain.Enums;

namespace Doska.Tests;

/// <summary>Правила Google-привязки и аккаунта без пароля.</summary>
public sealed class UserGoogleDomainTests
{
    private static readonly DateTimeOffset Created = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateWithGoogle_creates_confirmed_passwordless_account()
    {
        var user = User.CreateWithGoogle(
            "user@example.com",
            "  google-subject  ",
            "placeholder-hash",
            Created);

        Assert.Equal("user@example.com", user.Email);
        Assert.Equal(UserRole.User, user.Role);
        Assert.True(user.IsEmailConfirmed);
        Assert.False(user.HasPassword);
        Assert.Equal("google-subject", user.GoogleSubject);
        Assert.Equal(Created, user.CreatedAt);
        Assert.Equal(Created, user.UpdatedAt);
    }

    [Fact]
    public void Regular_account_has_password()
    {
        var user = new User("user@example.com", "hash", UserRole.User, Created);

        Assert.True(user.HasPassword);
        Assert.Null(user.GoogleSubject);
    }

    [Fact]
    public void LinkGoogle_stores_subject_and_updates_timestamp()
    {
        var user = new User("user@example.com", "hash", UserRole.User, Created);

        user.LinkGoogle("google-subject", Now);

        Assert.Equal("google-subject", user.GoogleSubject);
        Assert.Equal(Now, user.UpdatedAt);
    }

    [Fact]
    public void LinkGoogle_is_idempotent_for_same_subject()
    {
        var user = new User("user@example.com", "hash", UserRole.User, Created);
        user.LinkGoogle("google-subject", Created);

        user.LinkGoogle("google-subject", Now);

        Assert.Equal("google-subject", user.GoogleSubject);
        Assert.Equal(Created, user.UpdatedAt);
    }

    [Fact]
    public void LinkGoogle_rejects_another_subject()
    {
        var user = new User("user@example.com", "hash", UserRole.User, Created);
        user.LinkGoogle("first-subject", Created);

        Assert.Throws<InvalidOperationException>(() => user.LinkGoogle("second-subject", Now));
        Assert.Equal("first-subject", user.GoogleSubject);
        Assert.Equal(Created, user.UpdatedAt);
    }

    [Fact]
    public void LinkGoogle_rejects_blank_subject()
    {
        var user = new User("user@example.com", "hash", UserRole.User, Created);

        Assert.Throws<ArgumentException>(() => user.LinkGoogle("   ", Now));
        Assert.Null(user.GoogleSubject);
    }

    [Fact]
    public void SetPassword_enables_password_login()
    {
        var user = User.CreateWithGoogle("user@example.com", "google-subject", "placeholder", Created);

        user.SetPassword("new-hash", Now);

        Assert.True(user.HasPassword);
        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Equal(Now, user.UpdatedAt);
    }
}