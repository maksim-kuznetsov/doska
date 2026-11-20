using Doska.Application.Abstractions;
using Doska.Infrastructure.Email;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Doska.Tests;

/// <summary>Чтение писем из файлового ящика: список, заголовки и защита каталога.</summary>
public sealed class FileEmailOutboxTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        AppContext.BaseDirectory,
        "outbox-tests",
        Guid.NewGuid().ToString("N"));

    /// <summary>Удаляет созданный тестом каталог ящика.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task List_returns_messages_newest_first_with_headers()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

        WriteMessage("older.eml", "old@example.com", "Старое", now.AddHours(-2), now.AddHours(-2));
        WriteMessage("newer.eml", "new@example.com", "Новое", now, now);

        // Файл без заголовков не должен ломать список.
        var broken = Path.Combine(_directory, "broken.eml");
        await File.WriteAllTextAsync(broken, "не письмо", ct);
        File.SetLastWriteTimeUtc(broken, now.AddHours(-5).UtcDateTime);

        var messages = await CreateOutbox().ListAsync(ct);

        Assert.Equal(
            new[] { "newer.eml", "older.eml", "broken.eml" },
            messages.Select(message => message.FileName));

        Assert.Equal("new@example.com", messages[0].Recipient);
        Assert.Equal("Новое", messages[0].Subject);
        Assert.Equal(now, messages[0].SentAt);

        Assert.Equal(string.Empty, messages[2].Recipient);
        Assert.Null(messages[2].SentAt);
    }

    [Fact]
    public async Task List_limits_to_newest_messages()
    {
        var ct = TestContext.Current.CancellationToken;
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var index = 0; index < 205; index++)
        {
            WriteMessage(
                $"msg-{index:000}.eml",
                $"user{index}@example.com",
                $"Тема {index}",
                start,
                start.AddSeconds(index));
        }

        var messages = await CreateOutbox().ListAsync(ct);

        Assert.Equal(200, messages.Count);
        Assert.Equal("msg-204.eml", messages[0].FileName);
        Assert.Contains(messages, message => message.FileName == "msg-005.eml");
        Assert.DoesNotContain(messages, message => message.FileName == "msg-000.eml");
    }

    [Fact]
    public async Task List_returns_empty_when_directory_missing()
    {
        var messages = await CreateOutbox().ListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(messages);
    }

    [Fact]
    public async Task List_returns_empty_when_directory_empty()
    {
        Directory.CreateDirectory(_directory);

        var messages = await CreateOutbox().ListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(messages);
    }

    [Fact]
    public async Task Read_returns_content_for_existing_file()
    {
        var ct = TestContext.Current.CancellationToken;
        WriteMessage("mail.eml", "user@example.com", "Тема письма", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var result = await CreateOutbox().ReadAsync("mail.eml", ct);

        Assert.Equal(EmailOutboxReadStatus.Ok, result.Status);
        Assert.Contains("To: user@example.com", result.Text, StringComparison.Ordinal);
        Assert.Contains("Тема письма", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Read_returns_not_found_for_missing_file()
    {
        Directory.CreateDirectory(_directory);

        var result = await CreateOutbox().ReadAsync("missing.eml", TestContext.Current.CancellationToken);

        Assert.Equal(EmailOutboxReadStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("sub/secret.eml")]
    [InlineData("..")]
    [InlineData("")]
    public async Task Read_rejects_names_outside_the_outbox(string fileName)
    {
        Directory.CreateDirectory(_directory);

        var result = await CreateOutbox().ReadAsync(fileName, TestContext.Current.CancellationToken);

        Assert.Equal(EmailOutboxReadStatus.InvalidName, result.Status);
    }

    [Fact]
    public async Task Read_does_not_leak_files_from_parent_directory()
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(_directory);

        var parent = Path.GetDirectoryName(_directory)!;
        var secretName = $"secret-{Guid.NewGuid():N}.txt";
        var secretPath = Path.Combine(parent, secretName);
        await File.WriteAllTextAsync(secretPath, "секрет", ct);

        try
        {
            var result = await CreateOutbox().ReadAsync($"../{secretName}", ct);

            Assert.Equal(EmailOutboxReadStatus.InvalidName, result.Status);
        }
        finally
        {
            File.Delete(secretPath);
        }
    }

    private FileEmailOutbox CreateOutbox()
        => new(
            new StubHostEnvironment(AppContext.BaseDirectory),
            Options.Create(new EmailOptions { Mode = EmailModes.File, OutboxPath = _directory }),
            NullLogger<FileEmailOutbox>.Instance);

    private void WriteMessage(
        string fileName,
        string recipient,
        string subject,
        DateTimeOffset sentAt,
        DateTimeOffset modifiedAt)
    {
        Directory.CreateDirectory(_directory);

        var path = Path.Combine(_directory, fileName);
        var content = string.Join(
            "\r\n",
            "From: no-reply@doska.local",
            $"To: {recipient}",
            $"Subject: {subject}",
            $"Date: {sentAt:R}",
            "MIME-Version: 1.0",
            "Content-Type: text/plain; charset=utf-8",
            string.Empty,
            "Тело письма",
            string.Empty);

        File.WriteAllText(path, content);
        File.SetLastWriteTimeUtc(path, modifiedAt.UtcDateTime);
    }

    private sealed class StubHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Doska.Tests";

        public string ContentRootPath { get; set; } = contentRoot;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
