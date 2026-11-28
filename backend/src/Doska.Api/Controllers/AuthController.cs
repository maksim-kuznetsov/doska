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
public sealed class AuthController(
    AuthService authService,
    IExternalAuthProviderRegistry providers) : ControllerBase
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

    /// <summary>Сообщает, какие внешние провайдеры входа настроены.</summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    public ActionResult<AuthProvidersResponse> Providers()
        => Ok(new AuthProvidersResponse(
            providers.Providers
                .Select(provider => new AuthProviderInfo(
                    provider.Id,
                    provider.DisplayName,
                    provider.IsConfigured))
                .ToList()));

    /// <summary>Начинает вход через внешнего провайдера.</summary>
    [HttpGet("{provider}/start")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    public IActionResult Start(string provider, [FromQuery] string? redirect)
    {
        var resolved = ResolveProvider(provider);

        if (resolved is null)
        {
            return ProviderUnavailable();
        }

        return Challenge(
            ExternalAuthentication.LoginProperties(resolved.Id, redirect),
            resolved.Scheme);
    }

    /// <summary>Начинает привязку внешнего провайдера к текущему аккаунту.</summary>
    [HttpGet("{provider}/link")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    public IActionResult Link(string provider)
    {
        var resolved = ResolveProvider(provider);

        if (resolved is null)
        {
            return ProviderUnavailable();
        }

        return Challenge(
            ExternalAuthentication.LinkProperties(resolved.Id, User.GetRequiredUserId()),
            resolved.Scheme);
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

    /// <summary>Повторно отправляет письмо подтверждения; ответ не раскрывает состояние аккаунта.</summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.ConfirmationResend)]
    public async Task<IActionResult> ResendConfirmation(
        [FromBody] ResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        await authService.RequestEmailConfirmationResendAsync(request.Email, cancellationToken);
        return NoContent();
    }

    /// <summary>Запрашивает сброс пароля; ответ не раскрывает существование аккаунта.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.RequestPasswordResetAsync(request.Email, cancellationToken);
        return NoContent();
    }

    /// <summary>Устанавливает новый пароль по одноразовой ссылке.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request.Token, request.Password, cancellationToken);
        return NoContent();
    }

    private async Task SignInAsync(AuthenticatedUser user)
    {
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimsPrincipalExtensions.CreateSessionPrincipal(user));
    }

    private ObjectResult ProviderUnavailable()
        => NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Провайдер входа не настроен.",
        });

    /// <summary>Возвращает настроенного провайдера или <c>null</c>, если он неизвестен либо выключен.</summary>
    private ExternalAuthProvider? ResolveProvider(string provider)
    {
        var resolved = providers.Find(provider);
        return resolved is { IsConfigured: true } ? resolved : null;
    }
}
