using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Doska.Tests;

/// <summary>
/// Фабрика API для интеграционных тестов: фиксирует окружение Development,
/// чтобы работали CORS и Swagger, как при локальном запуске.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Интеграционные тесты не требуют запущенной базы: применение миграций при старте
        // проверяется отдельно, запуском API без базы.
        builder.UseSetting("Database:MigrateOnStartup", "false");
    }
}
