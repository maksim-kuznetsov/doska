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

/// <summary>
/// Общие события внешних провайдеров: нормализация идентичности к общему контракту,
/// завершение входа или привязки и коды ошибок.
/// </summary>
public static class ExternalAuthentication
{
    /// <summary>Временная cookie-схема внешнего входа, не участвующая в авторизации API.</summary>
    public const string ExternalScheme = "External";

    /// <summary>Пользовательский claim с идентификатором пользователя у провайдера.</summary>
    public const string SubjectClaim = "external_subject";

    /// <summary>Пользовательский claim с подтверждённым email провайдера.</summary>
    public const string EmailClaim = "external_email";

    /// <summary>Пользовательский claim с именем пользователя из провайдера.</summary>
    public const string NameClaim = "external_name";

    /// <summary>Пользовательский claim с признаком подтверждения email провайдера.</summary>
    public const string EmailVerifiedClaim = "external_email_verified";

    private const string ModeItem = "external_mode";
    private const string ProviderItem = "external_provider";
    private const string LinkUserIdItem = "external_link_user_id";
    private const string ReturnUrlItem = "external_return_url";
    private const string LoginMode = "login";
    private const string LinkMode = "link";
    private const string UnknownProvider = "external";

    /// <summary>Создаёт свойства начала входа через провайдера.</summary>
    public static AuthenticationProperties LoginProperties(string providerId, string? redirect)
    {
        var properties = new AuthenticationProperties();
        properties.Items[ModeItem] = LoginMode;
        properties.Items[ProviderItem] = providerId;
        properties.Items[ReturnUrlItem] = ExternalAuthRedirects.SafeLocalPath(redirect);
        return properties;
    }

    /// <summary>Создаёт свойства начала привязки провайдера к текущему пользователю.</summary>
    public static AuthenticationProperties LinkProperties(string providerId, Guid userId)
    {
        var properties = new AuthenticationProperties();
        properties.Items[ModeItem] = LinkMode;
        properties.Items[ProviderItem] = providerId;
        properties.Items[LinkUserIdItem] = userId.ToString();
        return properties;
    }

    /// <summary>Нормализованные данные внешней идентичности.</summary>
    /// <param name="Subject">Идентификатор пользователя у провайдера.</param>
    /// <param name="Email">Адрес электронной почты.</param>
    /// <param name="Name">Отображаемое имя.</param>
    /// <param name="EmailVerified">Признак подтверждения email провайдером.</param>
    public sealed record ExternalIdentity(string? Subject, string? Email, string? Name, bool EmailVerified);

    /// <summary>Извлекает идентичность Google из ответа userinfo.</summary>
    public static ExternalIdentity ParseGoogleIdentity(JsonElement user)
        => new(
            ReadString(user, "sub"),
            ReadString(user, "email"),
            ReadString(user, "name"),
            ReadBoolean(user, "email_verified"));

    /// <summary>Извлекает идентичность Яндекс ID из ответа профиля. Email Яндекса считается подтверждённым.</summary>
    public static ExternalIdentity ParseYandexIdentity(JsonElement user)
        => new(
            ReadString(user, "id"),
            ReadString(user, "default_email") ?? ReadFirstString(user, "emails"),
            ReadString(user, "real_name") ?? ReadString(user, "display_name"),
            EmailVerified: true);

    /// <summary>Извлекает идентичность Google из userinfo.</summary>
    public static Task GoogleOnCreatingTicket(OAuthCreatingTicketContext context)
    {
        var identity = ParseGoogleIdentity(context.User);
        FillIdentity(context, identity);
        return Task.CompletedTask;
    }

    /// <summary>Извлекает идентичность Яндекс ID из userinfo. Email Яндекса считается подтверждённым.</summary>
    public static Task YandexOnCreatingTicket(OAuthCreatingTicketContext context)
    {
        var identity = ParseYandexIdentity(context.User);
        FillIdentity(context, identity);
        return Task.CompletedTask;
    }

    /// <summary>Завершает вход или привязку и выписывает обычную cookie-сессию.</summary>
    public static async Task OnTicketReceived(TicketReceivedContext context)
    {
        var services = context.HttpContext.RequestServices;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Doska.ExternalAuth");
        var publicUrl = services.GetRequiredService<IPublicUrlProvider>();
        var externalAuth = services.GetRequiredService<ExternalAuthService>();

        var provider = ProviderOf(context.Properties);
        var mode = ModeOf(context.Properties);
        var failurePath = mode == LinkMode ? "/profile" : "/login";

        try
        {
            var principal = context.Principal;
            var subject = principal?.FindFirst(SubjectClaim)?.Value;
            var email = principal?.FindFirst(EmailClaim)?.Value;
            var displayName = principal?.FindFirst(NameClaim)?.Value;
            var emailVerified = bool.TryParse(
                principal?.FindFirst(EmailVerifiedClaim)?.Value,
                out var verified) && verified;

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            {
                RedirectFailure(context, publicUrl, failurePath, provider, "invalid");
                return;
            }

            if (!emailVerified)
            {
                RedirectFailure(context, publicUrl, failurePath, provider, "unverified");
                return;
            }

            AuthenticatedUser user;

            if (mode == LinkMode)
            {
                var linkUserIdValue = Item(context.Properties, LinkUserIdItem);

                if (!Guid.TryParse(linkUserIdValue, out var linkUserId))
                {
                    RedirectFailure(context, publicUrl, failurePath, provider, "invalid");
                    return;
                }

                var appAuth = await context.HttpContext.AuthenticateAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                var currentUserId = appAuth.Succeeded
                    ? appAuth.Principal?.GetUserIdOrNull()
                    : null;

                if (currentUserId is null || currentUserId.Value != linkUserId)
                {
                    RedirectFailure(context, publicUrl, failurePath, provider, "session");
                    return;
                }

                user = await externalAuth.LinkAsync(
                    linkUserId,
                    provider,
                    subject,
                    email,
                    displayName,
                    emailVerified,
                    context.HttpContext.RequestAborted);
            }
            else
            {
                user = await externalAuth.SignInAsync(
                    provider,
                    subject,
                    email,
                    displayName,
                    emailVerified,
                    context.HttpContext.RequestAborted);
            }

            await context.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimsPrincipalExtensions.CreateSessionPrincipal(user));

            var target = mode == LinkMode
                ? $"/profile?{Uri.EscapeDataString(provider)}=linked"
                : ExternalAuthRedirects.SafeLocalPath(Item(context.Properties, ReturnUrlItem));

            context.Response.Redirect(ExternalAuthRedirects.ToAbsolute(publicUrl.BaseUrl, target));
        }
        catch (ServiceException exception)
        {
            logger.LogWarning(exception, "Внешний вход отклонён: {Kind}.", exception.Kind);
            RedirectFailure(context, publicUrl, failurePath, provider, ReasonOf(exception));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Не удалось завершить внешний вход.");
            RedirectFailure(context, publicUrl, failurePath, provider, "failed");
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

    /// <summary>Обрабатывает отказ провайдера в доступе.</summary>
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
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Doska.ExternalAuth");
        var publicUrl = services.GetRequiredService<IPublicUrlProvider>();
        var provider = ProviderOf(properties);
        var mode = ModeOf(properties);
        var path = mode == LinkMode ? "/profile" : "/login";

        logger.LogWarning("Внешний вход не завершён: {Reason}.", reason);
        httpContext.Response.Redirect(
            ExternalAuthRedirects.ToAbsolute(
                publicUrl.BaseUrl,
                ExternalAuthRedirects.ErrorPath(path, provider, reason)));
        handleResponse();
    }

    private static void RedirectFailure(
        TicketReceivedContext context,
        IPublicUrlProvider publicUrl,
        string path,
        string provider,
        string reason)
    {
        context.Response.Redirect(
            ExternalAuthRedirects.ToAbsolute(
                publicUrl.BaseUrl,
                ExternalAuthRedirects.ErrorPath(path, provider, reason)));
    }

    private static void FillIdentity(OAuthCreatingTicketContext context, ExternalIdentity identity)
    {
        var target = context.Principal?.Identities.FirstOrDefault();

        if (target is null)
        {
            return;
        }

        ReplaceClaim(target, SubjectClaim, identity.Subject);
        ReplaceClaim(target, EmailClaim, identity.Email);
        ReplaceClaim(target, NameClaim, identity.Name);
        ReplaceClaim(target, EmailVerifiedClaim, identity.EmailVerified ? "true" : "false");
    }

    private static string ReasonOf(ServiceException exception)
        => exception.Kind switch
        {
            ServiceErrorKind.Forbidden => "blocked",
            ServiceErrorKind.Conflict => "conflict",
            ServiceErrorKind.Unauthorized => "session",
            _ => "invalid",
        };

    private static string ProviderOf(AuthenticationProperties? properties)
    {
        var provider = Item(properties, ProviderItem);
        return string.IsNullOrWhiteSpace(provider) ? UnknownProvider : provider;
    }

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
    {
        if (user.ValueKind != JsonValueKind.Object
            || !user.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
    }

    private static string? ReadFirstString(JsonElement user, string propertyName)
    {
        if (user.ValueKind != JsonValueKind.Object
            || !user.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                return item.GetString();
            }
        }

        return null;
    }

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
