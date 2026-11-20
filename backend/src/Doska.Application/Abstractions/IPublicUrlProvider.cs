namespace Doska.Application.Abstractions;

/// <summary>Публичный адрес сайта для абсолютных ссылок в письмах.</summary>
public interface IPublicUrlProvider
{
    /// <summary>Базовый адрес без завершающего слэша, например <c>https://doska.example.com</c>.</summary>
    string BaseUrl { get; }
}