using Doska.Infrastructure.Common;

namespace Doska.Tests;

/// <summary>Разрешение значения настройки пути в абсолютный путь.</summary>
public sealed class PathResolverTests
{
    [Fact]
    public void Resolve_keeps_absolute_path()
    {
        var absolute = Path.Combine(Path.GetTempPath(), "doska-media");

        Assert.Equal(absolute, PathResolver.Resolve(absolute, Path.GetTempPath()));
    }

    [Fact]
    public void Resolve_combines_relative_path_with_content_root()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), "doska-app");
        var configured = Path.Combine("media", "photos");

        Assert.Equal(Path.Combine(contentRoot, "media", "photos"), PathResolver.Resolve(configured, contentRoot));
    }
}
