using System.Net.Http.Json;

namespace Doska.Tests.Infrastructure;

/// <summary>Вызовы API с antiforgery-токеном, как это делает интерфейс.</summary>
internal static class ApiClientExtensions
{
    /// <summary>Отправляет изменяющий запрос с действующим antiforgery-токеном.</summary>
    public static async Task<HttpResponseMessage> PostJsonAsync(
        this HttpClient client,
        string url,
        object? body,
        CancellationToken cancellationToken)
    {
        return await SendAsync(client, HttpMethod.Post, url, body, cancellationToken);
    }

    /// <summary>Отправляет произвольное содержимое с действующим antiforgery-токеном.</summary>
    public static async Task<HttpResponseMessage> PostContentAsync(
        this HttpClient client,
        string url,
        HttpContent content,
        CancellationToken cancellationToken)
    {
        var csrf = await client.GetFromJsonAsync<CsrfPayload>("/api/auth/csrf", cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        return await client.SendAsync(request, cancellationToken);
    }

    /// <summary>Отправляет PUT с действующим antiforgery-токеном.</summary>
    public static async Task<HttpResponseMessage> PutJsonAsync(
        this HttpClient client,
        string url,
        object? body,
        CancellationToken cancellationToken)
    {
        return await SendAsync(client, HttpMethod.Put, url, body, cancellationToken);
    }

    /// <summary>Отправляет DELETE с действующим antiforgery-токеном.</summary>
    public static async Task<HttpResponseMessage> DeleteWithCsrfAsync(
        this HttpClient client,
        string url,
        CancellationToken cancellationToken)
    {
        return await SendAsync(client, HttpMethod.Delete, url, null, cancellationToken);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body,
        CancellationToken cancellationToken)
    {
        var csrf = await client.GetFromJsonAsync<CsrfPayload>("/api/auth/csrf", cancellationToken);

        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        return await client.SendAsync(request, cancellationToken);
    }

    private sealed record CsrfPayload(string Token);
}
