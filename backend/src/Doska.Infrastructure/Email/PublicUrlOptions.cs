namespace Doska.Infrastructure.Email;

/// <summary>Настройки публичного адреса сайта.</summary>
public sealed class PublicUrlOptions
{
    /// <summary>Имя секции конфигурации.</summary>
    public const string SectionName = "App";

    /// <summary>Адрес сайта, из которого строятся ссылки в письмах.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}