using Doska.Infrastructure.Common;
using Doska.Infrastructure.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Doska.Api.Controllers;

/// <summary>
/// Отдаёт фотографии объявлений. В Production используется внутренний редирект nginx,
/// в Development файл отдаётся приложением.
/// </summary>
[ApiController]
[Route("api/media/photos")]
[AllowAnonymous]
public sealed class MediaController(
    IHostEnvironment environment,
    IOptions<MediaOptions> options) : ControllerBase
{
    /// <summary>Возвращает фотографию или уменьшенную копию.</summary>
    [HttpGet("{adId:guid}/{fileName}")]
    public IActionResult Get(Guid adId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName is "." or ".."
            || fileName.Any(symbol => !(char.IsAsciiLetterOrDigit(symbol) || symbol is '.' or '_' or '-'))
            || Path.GetFileName(fileName) != fileName)
        {
            return NotFound();
        }

        var root = ResolveRoot();
        var photosRoot = Path.GetFullPath(Path.Combine(root, "photos"));
        var fullPath = Path.GetFullPath(Path.Combine(photosRoot, adId.ToString(), fileName));

        if (!fullPath.StartsWith(photosRoot, StringComparison.OrdinalIgnoreCase)
            || !System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        if (!environment.IsDevelopment())
        {
            Response.Headers["X-Accel-Redirect"] =
                $"{options.Value.InternalPrefix}/photos/{adId}/{fileName}";
            return Ok();
        }

        return PhysicalFile(fullPath, ContentTypeFor(Path.GetExtension(fileName)));
    }

    private string ResolveRoot()
        => PathResolver.Resolve(options.Value.RootPath, environment.ContentRootPath);

    private static string ContentTypeFor(string extension)
        => extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        };
}