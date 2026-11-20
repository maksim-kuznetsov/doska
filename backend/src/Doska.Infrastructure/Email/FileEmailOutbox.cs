using System.Globalization;
using Doska.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doska.Infrastructure.Email;

/// <summary>
/// Чтение писем, сохранённых <see cref="FileEmailSender"/> в режиме <c>Email:Mode=File</c>:
/// отдаёт последние письма и не выпускает чтение за пределы каталога ящика.
/// </summary>
public sealed class FileEmailOutbox(
    IHostEnvironment environment,
    IOptions<EmailOptions> options,
    ILogger<FileEmailOutbox> logger) : IEmailOutbox
{
    /// <summary>Сколько последних писем показывать в списке.</summary>
    private const int MaxMessages = 200;

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmailOutboxMessage>> ListAsync(CancellationToken cancellationToken = default)
    {
        var directory = ResolveDirectory();

        if (!Directory.Exists(directory))
        {
            return [];
        }

        var files = new DirectoryInfo(directory)
            .EnumerateFiles("*.eml", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Take(MaxMessages);

        var messages = new List<EmailOutboxMessage>();

        foreach (var file in files)
        {
            messages.Add(await DescribeAsync(file, cancellationToken));
        }

        return messages;
    }

    /// <inheritdoc />
    public async Task<EmailOutboxReadResult> ReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (!TryResolveFile(fileName, out var path))
        {
            return EmailOutboxReadResult.InvalidName();
        }

        if (!File.Exists(path))
        {
            return EmailOutboxReadResult.NotFound();
        }

        try
        {
            var text = await File.ReadAllTextAsync(path, cancellationToken);
            return EmailOutboxReadResult.Ok(text);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Не удалось прочитать письмо {Path}", path);
            return EmailOutboxReadResult.NotFound();
        }
    }

    private async Task<EmailOutboxMessage> DescribeAsync(FileInfo file, CancellationToken cancellationToken)
    {
        try
        {
            var text = await File.ReadAllTextAsync(file.FullName, cancellationToken);
            var headers = ParseHeaders(text);

            return new EmailOutboxMessage(
                file.Name,
                file.Length,
                file.LastWriteTimeUtc,
                headers.GetValueOrDefault("To") ?? string.Empty,
                headers.GetValueOrDefault("Subject") ?? string.Empty,
                ParseDate(headers.GetValueOrDefault("Date")));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Не удалось прочитать заголовки письма {Path}", file.FullName);

            return new EmailOutboxMessage(
                file.Name,
                file.Length,
                file.LastWriteTimeUtc,
                string.Empty,
                string.Empty,
                null);
        }
    }

    private string ResolveDirectory()
    {
        var configured = options.Value.OutboxPath;

        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);
    }

    /// <summary>
    /// Принимает только простое имя файла внутри каталога ящика: отвергает разделители пути,
    /// <c>..</c> и любой путь, ведущий за пределы каталога.
    /// </summary>
    private bool TryResolveFile(string fileName, out string path)
    {
        path = string.Empty;

        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
        {
            return false;
        }

        var root = Path.GetFullPath(ResolveDirectory());
        var prefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(root, fileName));

        if (!candidate.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        path = candidate;
        return true;
    }

    private static Dictionary<string, string> ParseHeaders(string content)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StringReader(content);
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0)
            {
                break;
            }

            var separator = line.IndexOf(':');

            if (separator <= 0)
            {
                continue;
            }

            headers.TryAdd(line[..separator].Trim(), line[(separator + 1)..].Trim());
        }

        return headers;
    }

    private static DateTimeOffset? ParseDate(string? value)
        => DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? parsed
            : null;
}
