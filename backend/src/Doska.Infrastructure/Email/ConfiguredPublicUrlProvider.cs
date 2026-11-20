using Doska.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Doska.Infrastructure.Email;

/// <summary>Публичный адрес из настройки <c>App:PublicBaseUrl</c>.</summary>
public sealed class ConfiguredPublicUrlProvider(IOptions<PublicUrlOptions> options) : IPublicUrlProvider
{
    /// <inheritdoc />
    public string BaseUrl { get; } = options.Value.PublicBaseUrl.TrimEnd('/');
}