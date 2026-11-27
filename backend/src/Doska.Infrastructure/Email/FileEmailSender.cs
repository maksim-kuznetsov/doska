using System.Text;
using Doska.Application.Abstractions;
using Doska.Infrastructure.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doska.Infrastructure.Email;

/// <summary>
/// Сохраняет письма как файлы <c>.eml</c> в каталоге исходящих. Режим для локальной
/// разработки: письмо можно открыть почтовым клиентом или текстовым редактором.
/// </summary>
public sealed class FileEmailSender(
    IHostEnvironment environment,
    IOptions<EmailOptions> options,
    ILogger<FileEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    public async Task SendAsync(
        string email,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var directory = ResolveDirectory();
        Directory.CreateDirectory(directory);

        var fileName = string.Join(
            '-',
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss"),
            Guid.NewGuid().ToString("N"),
            Sanitize(email)) + ".eml";

        var path = Path.Combine(directory, fileName);
        var content = string.Join(
            "\r\n",
            $"From: {SanitizeHeader(options.Value.From)}",
            $"To: {SanitizeHeader(email)}",
            $"Subject: {SanitizeHeader(subject)}",
            $"Date: {DateTimeOffset.UtcNow:R}",
            "MIME-Version: 1.0",
            "Content-Type: text/plain; charset=utf-8",
            string.Empty,
            body,
            string.Empty);

        await File.WriteAllTextAsync(path, content, new UTF8Encoding(false), cancellationToken);
        logger.LogInformation("Письмо сохранено в файл {Path}", path);
    }

    private string ResolveDirectory()
        => PathResolver.Resolve(options.Value.OutboxPath, environment.ContentRootPath);

    private static string Sanitize(string email)
    {
        var builder = new StringBuilder(email.Length);

        foreach (var symbol in email)
        {
            builder.Append(char.IsLetterOrDigit(symbol) ? symbol : '_');
        }

        return builder.ToString();
    }

    /// <summary>Удаляет управляющие символы, чтобы исключить инъекцию заголовков.</summary>
    private static string SanitizeHeader(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var symbol in value)
        {
            if (!char.IsControl(symbol))
            {
                builder.Append(symbol);
            }
        }

        return builder.ToString();
    }
}