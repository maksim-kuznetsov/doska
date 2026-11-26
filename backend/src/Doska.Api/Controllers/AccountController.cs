using Doska.Api.Contracts;
using Doska.Api.Infrastructure;
using Doska.Application.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Doska.Api.Controllers;

/// <summary>Профиль текущего пользователя: имя, контакты и смена пароля.</summary>
[ApiController]
[Route("api/account")]
[Authorize]
public sealed class AccountController(ProfileService profileService) : ControllerBase
{
    /// <summary>Сохраняет отображаемое имя и контакты текущего пользователя.</summary>
    [HttpPut("profile")]
    public async Task<ActionResult<UserResponse>> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
        => Ok(UserResponse.From(await profileService.UpdateAsync(
            User.GetRequiredUserId(),
            request.DisplayName,
            request.Contacts,
            cancellationToken)));

    /// <summary>Меняет пароль текущего пользователя, проверяя текущий пароль.</summary>
    [HttpPost("password")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await profileService.ChangePasswordAsync(
            User.GetRequiredUserId(),
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        return NoContent();
    }
}
