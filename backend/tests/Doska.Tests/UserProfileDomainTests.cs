using Doska.Domain.Entities;
using Doska.Domain.Enums;

namespace Doska.Tests;

/// <summary>Правила профиля пользователя.</summary>
public sealed class UserProfileDomainTests
{
    private static readonly DateTimeOffset Created = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UpdateProfile_trims_and_stores_values()
    {
        var user = new User("user@example.com", "hash", UserRole.User, Created);

        user.UpdateProfile("  Иван  ", "  +7 900 000-00-00  ", Now);

        Assert.Equal("Иван", user.DisplayName);
        Assert.Equal("+7 900 000-00-00", user.Contacts);
        Assert.Equal(Now, user.UpdatedAt);
    }

    [Fact]
    public void UpdateProfile_turns_blank_values_into_null()
    {
        var user = new User("user@example.com", "hash", UserRole.User, Created);
        user.UpdateProfile("Иван", "+7 900 000-00-00", Created);

        user.UpdateProfile("   ", null, Now);

        Assert.Null(user.DisplayName);
        Assert.Null(user.Contacts);
    }

    [Fact]
    public void New_user_has_no_profile()
    {
        var user = new User("user@example.com", "hash", UserRole.User, Created);

        Assert.Null(user.DisplayName);
        Assert.Null(user.Contacts);
    }
}
