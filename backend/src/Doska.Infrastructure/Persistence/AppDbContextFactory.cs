using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Doska.Infrastructure.Persistence;

/// <summary>
/// Контекст для команд <c>dotnet-ef</c>: инструменты не запускают API и не читают
/// <c>appsettings.Development.json</c> автоматически, поэтому строка подключения
/// берётся из переменной окружения <c>ConnectionStrings__Default</c>, а если её нет —
/// из <c>appsettings.Development.json</c> проекта <c>Doska.Api</c>.
/// <para>
/// Схема меняется только миграциями: новая миграция создаётся из каталога <c>backend</c>
/// командой
/// <c>dotnet tool run dotnet-ef migrations add ИмяМиграции --project src/Doska.Infrastructure --startup-project src/Doska.Api --output-dir Migrations</c>,
/// применение — <c>dotnet tool run dotnet-ef database update --project src/Doska.Infrastructure --startup-project src/Doska.Api</c>.
/// Ручные правки схемы в базе запрещены.
/// </para>
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConnectionStringVariable = "ConnectionStrings__Default";
    private const string DevelopmentSettingsFileName = "appsettings.Development.json";

    /// <inheritdoc />
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ResolveConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options, TimeProvider.System);
    }

    private static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var settingsPath = FindDevelopmentSettings();
        if (settingsPath is null)
        {
            throw new InvalidOperationException(
                $"Не найдена строка подключения: задайте переменную окружения {ConnectionStringVariable} " +
                $"или создайте {DevelopmentSettingsFileName} в проекте Doska.Api.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
        var connectionString = document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("Default")
            .GetString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"В {settingsPath} не заполнено ConnectionStrings:Default.");
        }

        return connectionString;
    }

    private static string? FindDevelopmentSettings()
    {
        foreach (var root in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(root);

            while (directory is not null)
            {
                foreach (var relativePath in new[]
                         {
                             Path.Combine("src", "Doska.Api", DevelopmentSettingsFileName),
                             DevelopmentSettingsFileName,
                         })
                {
                    var candidate = Path.Combine(directory.FullName, relativePath);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }

                directory = directory.Parent;
            }
        }

        return null;
    }
}
