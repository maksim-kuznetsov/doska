using Doska.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Tests.Infrastructure;

/// <summary>Доступ к базе данных в интеграционных тестах.</summary>
internal static class TestDatabase
{
    public const string SkipReason =
        "PostgreSQL недоступен для этого окружения: запустите 'docker compose up -d postgres' из корня проекта.";

    /// <summary>Проверяет доступность базы.</summary>
    public static async Task<bool> IsReachableAsync(ApiFactory factory, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await context.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Выполняет действие с контекстом базы.</summary>
    public static async Task<T> WithContextAsync<T>(
        ApiFactory factory,
        Func<AppDbContext, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(context);
    }

    /// <summary>Выполняет действие с контекстом базы.</summary>
    public static async Task WithContextAsync(ApiFactory factory, Func<AppDbContext, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(context);
    }

    /// <summary>Выполняет действие в области видимости сервисов приложения.</summary>
    public static async Task<T> WithServicesAsync<T>(
        ApiFactory factory,
        Func<IServiceProvider, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }
}
