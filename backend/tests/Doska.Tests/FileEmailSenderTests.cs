using Doska.Infrastructure.Email;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Doska.Tests;

/// <summary>Файловый отправитель писем сохраняет письмо как <c>.eml</c>.</summary>
public sealed class FileEmailSenderTests
{
    [Fact]
    public async Task SendAsync_writes_eml_file_with_recipient_subject_and_body()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = Path.Combine(
            AppContext.BaseDirectory,
            "email-tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var options = Options.Create(new EmailOptions
            {
                Mode = EmailModes.File,
                OutboxPath = directory,
                From = "no-reply@doska.local",
            });

            var sender = new FileEmailSender(
                new StubHostEnvironment(AppContext.BaseDirectory),
                options,
                NullLogger<FileEmailSender>.Instance);

            await sender.SendAsync(
                "user@example.com",
                "Объявление отклонено",
                "Причина: спам",
                cancellationToken);

            var file = Assert.Single(Directory.GetFiles(directory, "*.eml"));
            var content = await File.ReadAllTextAsync(file, cancellationToken);

            Assert.Contains("To: user@example.com", content, StringComparison.Ordinal);
            Assert.Contains("Subject: Объявление отклонено", content, StringComparison.Ordinal);
            Assert.Contains("Причина: спам", content, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private sealed class StubHostEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Doska.Tests";

        public string ContentRootPath { get; set; } = contentRoot;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}