using System.Security.Claims;
using Doska.Api.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Doska.Api.Infrastructure;
using Doska.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Doska.Api.Controllers;

/// <summary>Регистрация, вход, выход и подтверждение email.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>Выдаёт antiforgery-токен для изменяющих запросов.</summary>
    [HttpGet("csrf")]
    [AllowAnonymous]
    public ActionResult<CsrfResponse> Csrf([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new CsrfResponse(tokens.RequestToken ?? string.Empty));
    }

    /// <summary>Регистрирует аккаунт и открывает сессию.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    public async Task<ActionResult<UserResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(request.Email, request.Password, cancellationToken);
        await SignInAsync(user);
        return Ok(UserResponse.From(user));
    }

    /// <summary>Выполняет вход по email и паролю.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    public async Task<ActionResult<UserResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        await SignInAsync(user);
        return Ok(UserResponse.From(user));
    }

    /// <summary>Завершает сессию.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>Возвращает сведения о текущем пользователе.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        var user = await authService.GetAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(UserResponse.From(user));
    }

    /// <summary>Подтверждает email по одноразовому коду.</summary>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ConfirmEmailAsync(request.Token, cancellationToken);
        return NoContent();
    }

    private async Task SignInAsync(AuthenticatedUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
