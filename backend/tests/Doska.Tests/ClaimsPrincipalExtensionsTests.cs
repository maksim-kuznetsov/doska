using System.Security.Claims;
using Doska.Api.Infrastructure;
using Doska.Application.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Doska.Tests;

/// <summary>Построение принципала сессии из сведений о пользователе.</summary>
public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void CreateSessionPrincipal_sets_expected_claims_and_scheme()
    {
        var user = new AuthenticatedUser(
            Guid.NewGuid(),
            "moderator@example.com",
            "Moderator",
            IsTrusted: true,
            IsEmailConfirmed: true,
            DisplayName: "Модератор",
            Contacts: null);

        var principal = ClaimsPrincipalExtensions.CreateSessionPrincipal(user);

        Assert.Equal(user.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(user.Email, principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(user.Role, principal.FindFirstValue(ClaimTypes.Role));

        var identity = Assert.Single(principal.Identities);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, identity.AuthenticationType);
    }
}
