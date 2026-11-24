using System.Net;
using System.Net.Http.Json;
using Doska.Application.Abstractions;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Tests;

/// <summary>
/// Проверяет, что в production API доверяет заголовкам прокси:
/// запрос с X-Forwarded-Proto: https получает сессионную cookie с флагом Secure.
/// </summary>
public sealed class ForwardedHeadersTests
{
    private const string Password = "secret123";

    [Fact]
    public async Task Https_forwarded_proto_sets_secure_session_cookie()
    {
        using var factory = new ProductionFactory();
        var ct = TestContext.Current.CancellationToken;

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Database.CanConnectAsync(ct))
        {
            Assert.Skip(
                "PostgreSQL недоступен для этого окружения: запустите 'docker compose up -d postgres' из корня проекта.");
        }

        var email = $"forwarded-{Guid.NewGuid():N}@example.com";
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        db.Users.Add(TestData.CreateUser(
            email,
            UserRole.User,
            passwordHash: hasher.Hash(Password)));
        await db.SaveChangesAsync(ct);

        try
        {
            using var client = factory.CreateClient();

            var secureCookie = await LoginAsync(client, email, "https", ct);
            Assert.Contains("secure", secureCookie, StringComparison.OrdinalIgnoreCase);

            var plainCookie = await LoginAsync(client, email, null, ct);
            Assert.DoesNotContain("secure", plainCookie, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await db.Users.Where(user => user.Email == email).ExecuteDeleteAsync(ct);
        }
    }

    private static async Task<string> LoginAsync(
        HttpClient client,
        string email,
        string? forwardedProto,
        CancellationToken cancellationToken)
    {
        var csrf = await client.GetFromJsonAsync<CsrfPayload>("/api/auth/csrf", cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = Password }),
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        if (forwardedProto is not null)
        {
            request.Headers.Add("X-Forwarded-Proto", forwardedProto);
        }

        var response = await client.SendAsync(request, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values
            : [];

        return Assert.Single(cookies, cookie => cookie.StartsWith("doska.session=", StringComparison.Ordinal));
    }

    private sealed record CsrfPayload(string Token);

    private sealed class ProductionFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.UseSetting("Ads:ArchiveBackgroundService", "false");
            builder.UseSetting(
                "ConnectionStrings:Default",
                "Host=localhost;Port=5433;Database=doska;Username=doska;Password=doska");
            builder.UseSetting(
                "Media:RootPath",
                Path.Combine(AppContext.BaseDirectory, "test-media"));
            builder.UseSetting("App:PublicBaseUrl", "https://doska.example.com");
            builder.UseSetting("Email:Mode", "Smtp");
            builder.UseSetting("Email:Host", "smtp.example.com");
            builder.UseSetting("Email:From", "no-reply@doska.example.com");
            builder.UseSetting("Logging:EventLog:LogLevel:Default", "None");

            builder.ConfigureServices(services => services.AddSingleton<IEmailSender>(new CapturingEmailSender()));
        }
    }
}