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
    }
}
