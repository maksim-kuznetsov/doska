using Doska.Domain.Entities;
using Doska.Domain.Enums;

namespace Doska.Tests;

/// <summary>Правила внешней идентичности и аккаунта без пароля.</summary>
public sealed class ExternalLoginDomainTests
{
    private static readonly DateTimeOffset Created = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreatePasswordless_creates_confirmed_account_without_password()
    {
        var user = User.CreatePasswordless("user@example.com", "placeholder-hash", Created);

        Assert.Equal("user@example.com", user.Email);
        Assert.Equal(UserRole.User, user.Role);
        Assert.True(user.IsEmailConfirmed);
        Assert.False(user.HasPassword);
        Assert.Equal(Created, user.CreatedAt);
        Assert.Equal(Created, user.UpdatedAt);
    }

    [Fact]
    public void Regular_account_has_password()
    {
        var user = new User("user@example.com", "hash", UserRole.User, Created);

        Assert.True(user.HasPassword);
    }

    [Fact]
    public void External_login_normalizes_provider_and_subject_and_records_time()
    {
        var userId = Guid.CreateVersion7();

        var login = new ExternalLogin(userId, "  Google  ", "  subject-1  ", Created);

        Assert.Equal(userId, login.UserId);
        Assert.Equal("google", login.Provider);
        Assert.Equal("subject-1", login.Subject);
        Assert.Equal(Created, login.CreatedAt);
        Assert.Equal(Created, login.UpdatedAt);
    }

    [Theory]
    [InlineData("", "subject")]
    [InlineData("google", "")]
    [InlineData("   ", "subject")]
    public void External_login_rejects_blank_parts(string provider, string subject)
        => Assert.Throws<ArgumentException>(
            () => new ExternalLogin(Guid.CreateVersion7(), provider, subject, Created));

    [Fact]
    public void External_login_rejects_empty_user_id()
        => Assert.Throws<ArgumentException>(
            () => new ExternalLogin(Guid.Empty, "google", "subject", Created));

    [Fact]
    public void SetPassword_enables_password_login()
    {
        var user = User.CreatePasswordless("user@example.com", "placeholder", Created);

        user.SetPassword("new-hash", Now);

        Assert.True(user.HasPassword);
        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Equal(Now, user.UpdatedAt);
    }

    [Fact]
    public void ApplyExternalDisplayName_fills_empty_name_and_updates_timestamp()
    {
        var user = User.CreatePasswordless("user@example.com", "placeholder", Created);

        user.ApplyExternalDisplayName("  Иван Иванов  ", Now);

        Assert.Equal("Иван Иванов", user.DisplayName);
        Assert.Equal(Now, user.UpdatedAt);
    }

    [Fact]
    public void ApplyExternalDisplayName_does_not_overwrite_existing_name()
    {
        var user = User.CreatePasswordless("user@example.com", "placeholder", Created);
        user.UpdateProfile("Заданное имя", null, Created);

        user.ApplyExternalDisplayName("Другое имя", Now);

        Assert.Equal("Заданное имя", user.DisplayName);
        Assert.Equal(Created, user.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ApplyExternalDisplayName_ignores_blank_name(string? displayName)
    {
        var user = User.CreatePasswordless("user@example.com", "placeholder", Created);

        user.ApplyExternalDisplayName(displayName, Now);

        Assert.Null(user.DisplayName);
        Assert.Equal(Created, user.UpdatedAt);
    }

    [Fact]
    public void ApplyExternalDisplayName_ignores_too_long_name()
    {
        var user = User.CreatePasswordless("user@example.com", "placeholder", Created);
        var tooLong = new string('я', User.MaxDisplayNameLength + 1);

        user.ApplyExternalDisplayName(tooLong, Now);

        Assert.Null(user.DisplayName);
        Assert.Equal(Created, user.UpdatedAt);
    }
}
