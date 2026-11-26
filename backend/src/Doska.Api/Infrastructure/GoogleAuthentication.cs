using System.Security.Claims;
using System.Text.Json;
using Doska.Application.Abstractions;
using Doska.Application.Auth;
using Doska.Application.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Doska.Api.Infrastructure;

/// <summary>События Google-хендлера: извлечение идентичности, вход и привязка.</summary>
public static class GoogleAuthentication
{
    /// <summary>Временная cookie-схема, не участвующая в авторизации API.</summary>
    public const string ExternalScheme = "GoogleExternal";

    /// <summary>Пользовательский claim с Google subject.</summary>
    public const string SubjectClaim = "google_subject";

    /// <summary>Пользовательский claim с подтверждённым email Google.</summary>
    public const string EmailClaim = "google_email";

    /// <summary>Пользовательский claim с признаком подтверждения email Google.</summary>
    public const string EmailVerifiedClaim = "google_email_verified";

    private const string ModeItem = "google_mode";
    private const string LinkUserIdItem = "google_link_user_id";
    private const string ReturnUrlItem = "google_return_url";
    private const string LoginMode = "login";
    private const string LinkMode = "link";

    /// <summary>Создаёт свойства начала входа.</summary>
    public static AuthenticationProperties LoginProperties(string? redirect)
    {
        var properties = new AuthenticationProperties();
        properties.Items[ModeItem] = LoginMode;
        properties.Items[ReturnUrlItem] = GoogleRedirects.SafeLocalPath(redirect);
        return properties;
    }

    /// <summary>Создаёт свойства начала привязки Google к текущему пользователю.</summary>
    public static AuthenticationProperties LinkProperties(Guid userId)
    {
        var properties = new AuthenticationProperties();
        properties.Items[ModeItem] = LinkMode;
        properties.Items[LinkUserIdItem] = userId.ToString();
        return properties;
    }

    /// <summary>Извлекает Google subject, email и подтверждение email из userinfo.</summary>
    public static Task OnCreatingTicket(OAuthCreatingTicketContext context)
    {
        var identity = context.Principal?.Identities.FirstOrDefault();

        if (identity is not null)
        {
            ReplaceClaim(identity, SubjectClaim, ReadString(context.User, "sub"));
            ReplaceClaim(identity, EmailClaim, ReadString(context.User, "email"));
            ReplaceClaim(
                identity,
                EmailVerifiedClaim,
                ReadBoolean(context.User, "email_verified") ? "true" : "false");
        }

        return Task.CompletedTask;
    }

    /// <summary>Завершает вход или привязку и выписывает обычную cookie-сессию.</summary>
    public static async Task OnTicketReceived(TicketReceivedContext context)
    {
        var services = context.HttpContext.RequestServices;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Doska.GoogleAuth");
        var publicUrl = services.GetRequiredService<IPublicUrlProvider>();
        var googleAuth = services.GetRequiredService<GoogleAuthService>();

        var mode = ModeOf(context.Properties);
        var failurePath = mode == LinkMode ? "/profile" : "/login";

        try
        {
            var principal = context.Principal;
            var subject = principal?.FindFirst(SubjectClaim)?.Value;
            var email = principal?.FindFirst(EmailClaim)?.Value;
            var emailVerified = bool.TryParse(
                principal?.FindFirst(EmailVerifiedClaim)?.Value,
                out var verified) && verified;

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            {
                RedirectFailure(context, publicUrl, failurePath, "invalid");
                return;
            }

            if (!emailVerified)
            {
                RedirectFailure(context, publicUrl, failurePath, "unverified");
                return;
            }

            AuthenticatedUser user;

            if (mode == LinkMode)
            {
                var linkUserIdValue = Item(context.Properties, LinkUserIdItem);

                if (!Guid.TryParse(linkUserIdValue, out var linkUserId))
                {
                    RedirectFailure(context, publicUrl, failurePath, "invalid");
                    return;
                }

                var appAuth = await context.HttpContext.AuthenticateAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                var currentUserId = appAuth.Succeeded
                    ? appAuth.Principal?.GetUserIdOrNull()
                    : null;

                if (currentUserId is null || currentUserId.Value != linkUserId)
                {
                    RedirectFailure(context, publicUrl, failurePath, "session");
                    return;
                }

                user = await googleAuth.LinkAsync(
                    linkUserId,
                    subject,
                    email,
                    emailVerified,
                    context.HttpContext.RequestAborted);
            }
            else
            {
                user = await googleAuth.SignInAsync(
                    subject,
                    email,
                    emailVerified,
                    context.HttpContext.RequestAborted);
            }

            await context.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimsPrincipalExtensions.CreateSessionPrincipal(user));

            var target = mode == LinkMode
                ? "/profile?google=linked"
                : GoogleRedirects.SafeLocalPath(Item(context.Properties, ReturnUrlItem));

            context.Response.Redirect(GoogleRedirects.ToAbsolute(publicUrl.BaseUrl, target));
        }
        catch (ServiceException exception)
        {
            logger.LogWarning(exception, "Google-вход отклонён: {Kind}.", exception.Kind);
            RedirectFailure(context, publicUrl, failurePath, ReasonOf(exception));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Не удалось завершить вход через Google.");
            RedirectFailure(context, publicUrl, failurePath, "failed");
        }
        finally
        {
            context.HandleResponse();
        }
    }

    /// <summary>Возвращает пользователя на страницу входа или профиля с кодом ошибки.</summary>
    public static Task OnRemoteFailure(RemoteFailureContext context)
    {
        HandleRemoteFailureCore(
            context.HttpContext,
            context.Properties,
            "failed",
            context.HandleResponse);
        return Task.CompletedTask;
    }

    /// <summary>Обрабатывает отказ Google в доступе.</summary>
    public static Task OnAccessDenied(AccessDeniedContext context)
    {
        HandleRemoteFailureCore(
            context.HttpContext,
            context.Properties,
            "denied",
            context.HandleResponse);
        return Task.CompletedTask;
    }

    private static void HandleRemoteFailureCore(
        HttpContext httpContext,
        AuthenticationProperties? properties,
        string reason,
        Action handleResponse)
    {
        var services = httpContext.RequestServices;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Doska.GoogleAuth");
        var publicUrl = services.GetRequiredService<IPublicUrlProvider>();
        var mode = ModeOf(properties);
        var path = mode == LinkMode ? "/profile" : "/login";

        logger.LogWarning("Google-вход не завершён: {Reason}.", reason);
        httpContext.Response.Redirect(
            GoogleRedirects.ToAbsolute(publicUrl.BaseUrl, GoogleRedirects.ErrorPath(path, reason)));
        handleResponse();
    }

    private static void RedirectFailure(
        TicketReceivedContext context,
        IPublicUrlProvider publicUrl,
        string path,
        string reason)
    {
        context.Response.Redirect(
            GoogleRedirects.ToAbsolute(publicUrl.BaseUrl, GoogleRedirects.ErrorPath(path, reason)));
    }

    private static string ReasonOf(ServiceException exception)
        => exception.Kind switch
        {
            ServiceErrorKind.Forbidden => "blocked",
            ServiceErrorKind.Conflict => "conflict",
            ServiceErrorKind.Unauthorized => "session",
            _ => "invalid",
        };

    private static string ModeOf(AuthenticationProperties? properties)
        => string.Equals(Item(properties, ModeItem), LinkMode, StringComparison.Ordinal)
            ? LinkMode
            : LoginMode;

    private static string? Item(AuthenticationProperties? properties, string key)
        => properties?.Items.TryGetValue(key, out var value) == true ? value : null;

    private static void ReplaceClaim(ClaimsIdentity identity, string type, string? value)
    {
        foreach (var existing in identity.FindAll(type).ToList())
        {
            identity.RemoveClaim(existing);
        }

        if (!string.IsNullOrEmpty(value))
        {
            identity.AddClaim(new Claim(type, value));
        }
    }

    private static string? ReadString(JsonElement user, string propertyName)
        => user.ValueKind == JsonValueKind.Object
            && user.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

    private static bool ReadBoolean(JsonElement user, string propertyName)
    {
        if (user.ValueKind != JsonValueKind.Object
            || !user.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false,
        };
    }
}