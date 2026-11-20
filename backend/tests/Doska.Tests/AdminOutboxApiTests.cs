using System.Net;
using System.Net.Http.Json;
using Doska.Application.Abstractions;
using Doska.Domain.Entities;
using Doska.Domain.Enums;
using Doska.Infrastructure.Persistence;
using Doska.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Tests;

/// <summary>Чтение файлового ящика писем администратором.</summary>
public sealed class AdminOutboxApiTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string Password = "secret123";

    private readonly ApiFactory _factory;

    public AdminOutboxApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Тесты не требуют подготовки.</summary>
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    /// <summary>Удаляет данные, созданные тестом.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Directory.Exists(_factory.OutboxPath))
        {
            Directory.Delete(_factory.OutboxPath, recursive: true);
        }

        var ct = TestContext.Current.CancellationToken;

        if (!await TestDatabase.IsReachableAsync(_factory, ct))
        {
            return;
        }

        await TestDatabase.WithContextAsync(_factory, async db =>
        {
            db.Users.RemoveRange(db.Users.Where(user => user.Email.StartsWith("outbox-")));
            await db.SaveChangesAsync(ct);
        });
    }

    [Fact]
    public async Task Anonymous_gets_unauthorized()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/admin/outbox", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Non_admin_gets_forbidden()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var moderator = _factory.CreateClient();
        await LoginAsync(moderator, await CreateStaffAsync(UserRole.Moderator), ct);
        Assert.Equal(HttpStatusCode.Forbidden, (await moderator.GetAsync("/api/admin/outbox", ct)).StatusCode);

        using var user = _factory.CreateClient();
        await LoginAsync(user, await CreateStaffAsync(UserRole.User), ct);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/admin/outbox", ct)).StatusCode);
    }

    [Fact]
    public async Task Admin_lists_messages_and_reads_content()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;
        var fileName = WriteOutboxMessage("Письмо подтверждения", "user@example.com");

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var list = await admin.GetFromJsonAsync<List<OutboxMessagePayload>>("/api/admin/outbox", ct);
        var summary = Assert.Single(list!, item => item.FileName == fileName);
        Assert.Equal("user@example.com", summary.Recipient);
        Assert.Equal("Письмо подтверждения", summary.Subject);
        Assert.NotNull(summary.SentAt);

        var content = await admin.GetFromJsonAsync<OutboxContentPayload>(
            $"/api/admin/outbox/{fileName}",
            ct);

        Assert.Contains("To: user@example.com", content!.Content, StringComparison.Ordinal);
        Assert.Contains("token=abc", content.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Traversal_returns_bad_request()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        // Имя с разделителем пути должно дойти до проверки и быть отклонено кодом 400.
        var response = await admin.GetAsync("/api/admin/outbox/sub/mail.eml", ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Missing_file_returns_not_found()
    {
        await EnsureDatabaseAsync();
        var ct = TestContext.Current.CancellationToken;

        using var admin = _factory.CreateClient();
        await LoginAsync(admin, await CreateStaffAsync(UserRole.Admin), ct);

        var response = await admin.GetAsync($"/api/admin/outbox/missing-{Guid.NewGuid():N}.eml", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task EnsureDatabaseAsync()
    {
        if (!await TestDatabase.IsReachableAsync(_factory, TestContext.Current.CancellationToken))
        {
            Assert.Skip(TestDatabase.SkipReason);
        }
    }

    private string WriteOutboxMessage(string subject, string recipient)
    {
        Directory.CreateDirectory(_factory.OutboxPath);

        var fileName = $"{Guid.NewGuid():N}.eml";
        var content = string.Join(
            "\r\n",
            "From: no-reply@doska.local",
            $"To: {recipient}",
            $"Subject: {subject}",
            $"Date: {DateTimeOffset.UtcNow:R}",
            "MIME-Version: 1.0",
            "Content-Type: text/plain; charset=utf-8",
            string.Empty,
            "Перейдите по ссылке https://doska.example.com/confirm-email?token=abc",
            string.Empty);

        File.WriteAllText(Path.Combine(_factory.OutboxPath, fileName), content);
        return fileName;
    }

    private async Task LoginAsync(HttpClient client, string email, CancellationToken cancellationToken)
    {
        var response = await client.PostJsonAsync(
            "/api/auth/login",
            new { email, password = Password },
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> CreateStaffAsync(UserRole role)
    {
        var email = $"outbox-{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

        await TestDatabase.WithServicesAsync(_factory, async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var passwordHasher = provider.GetRequiredService<IPasswordHasher>();

            db.Users.Add(new User
            {
                Email = email,
                PasswordHash = passwordHasher.Hash(Password),
                Role = role,
                IsEmailConfirmed = true,
            });

            await db.SaveChangesAsync();
            return 0;
        });

        return email;
    }

    private sealed record OutboxMessagePayload(
        string FileName,
        long Size,
        DateTimeOffset ModifiedAt,
        string Recipient,
        string Subject,
        DateTimeOffset? SentAt);

    private sealed record OutboxContentPayload(string Content);
}
