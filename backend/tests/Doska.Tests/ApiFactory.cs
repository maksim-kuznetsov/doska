using Doska.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Tests;

/// <summary>
/// Фабрика API для интеграционных тестов: фиксирует окружение Development,
/// чтобы работали CORS и Swagger, как при локальном запуске.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Сборщик отправленных писем: подменяет настоящую отправку.</summary>
    public CapturingEmailSender Emails { get; } = new();

    /// <summary>Каталог файлового ящика писем для тестов (свой у каждой фабрики).</summary>
    public string OutboxPath { get; } = Path.Combine(
        AppContext.BaseDirectory,
        "test-outbox",
        Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Интеграционные тесты не требуют запущенной базы: применение миграций при старте
        // проверяется отдельно, запуском API без базы.
        builder.UseSetting("Database:MigrateOnStartup", "false");

        // Фоновый автоархив иначе изменит статусы тестовых объявлений с историческими датами.
        builder.UseSetting("Ads:ArchiveBackgroundService", "false");

        // Медиа пишутся в каталог сборки тестов, чтобы не засорять проект.
        builder.UseSetting(
            "Media:RootPath",
            Path.Combine(AppContext.BaseDirectory, "test-media"));

        // Файловый ящик писем — тоже в каталог сборки тестов.
        builder.UseSetting("Email:OutboxPath", OutboxPath);

        // Абсолютный адрес сайта: письма должны содержать рабочие ссылки.
        builder.UseSetting("App:PublicBaseUrl", "https://doska.example.com");

        builder.ConfigureServices(services => services.AddSingleton<IEmailSender>(Emails));
    }
}
