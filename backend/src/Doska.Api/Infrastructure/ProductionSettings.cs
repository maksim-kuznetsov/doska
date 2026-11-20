using Doska.Infrastructure.Email;

namespace Doska.Api.Infrastructure;

/// <summary>Проверяет обязательные настройки production до запуска приложения.</summary>
public static class ProductionSettings
{
    /// <summary>Проверяет публичный адрес и режим отправки писем.</summary>
    public static void Validate(
        IConfiguration configuration,
        IHostEnvironment environment,
        string emailMode)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var publicBaseUrl = configuration["App:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            throw new InvalidOperationException(
                "App:PublicBaseUrl обязателен в Production: укажите публичный адрес сайта, " +
                "например https://doska.example.com.");
        }

        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "App:PublicBaseUrl должен быть абсолютным адресом http или https, " +
                $"например https://doska.example.com. Текущее значение: {publicBaseUrl}.");
        }

        if (!string.Equals(emailMode, EmailModes.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "В Production письма отправляются через SMTP: задайте Email:Mode=Smtp " +
                "и параметры Email:Host, Email:Port, Email:From.");
        }

        if (string.IsNullOrWhiteSpace(configuration["Email:Host"]))
        {
            throw new InvalidOperationException(
                "Email:Host обязателен в Production: укажите SMTP-хост провайдера.");
        }
    }
}