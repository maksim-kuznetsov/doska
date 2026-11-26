using System.Text;

namespace Doska.Api.Infrastructure;

/// <summary>Безопасные адреса возврата после Google-входа.</summary>
public static class GoogleRedirects
{
    private const int MaxPathLength = 2048;

    /// <summary>
    /// Возвращает локальный путь возврата или <c>/</c>, если адрес внешний либо небезопасный.
    /// </summary>
    public static string SafeLocalPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/";
        }

        var path = value.Trim();

        if (path.Length > MaxPathLength
            || path.Any(char.IsControl)
            || path.Contains('\\')
            || !path.StartsWith('/')
            || path.StartsWith("//", StringComparison.Ordinal))
        {
            return "/";
        }

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(path);
        }
        catch (UriFormatException)
        {
            return "/";
        }

        if (decoded.Any(char.IsControl)
            || decoded.Contains('\\')
            || !decoded.StartsWith('/')
            || decoded.StartsWith("//", StringComparison.Ordinal))
        {
            return "/";
        }

        return path;
    }

    /// <summary>Собирает абсолютный адрес сайта из публичного адреса и локального пути.</summary>
    public static string ToAbsolute(string publicBaseUrl, string path)
    {
        var normalizedPath = SafeLocalPath(path);
        var baseUrl = (publicBaseUrl ?? string.Empty).TrimEnd('/');

        if (baseUrl.Length == 0)
        {
            return normalizedPath;
        }

        return $"{baseUrl}{normalizedPath}";
    }

    /// <summary>Собирает адрес с query-параметрами ошибки Google-входа.</summary>
    public static string ErrorPath(string path, string reason)
    {
        var builder = new StringBuilder(SafeLocalPath(path));
        builder.Append(path.Contains('?') ? '&' : '?');
        builder.Append("google=error&reason=");
        builder.Append(Uri.EscapeDataString(reason));
        return builder.ToString();
    }
}