using System.Net.Sockets;
using Doska.Domain.Entities;
using Doska.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Doska.Tests;

/// <summary>
/// Проверка владения отметками времени доменом: <c>created_at</c> задаёт конструктор,
/// <c>updated_at</c> — доменный метод, а сохранение в базу их не проставляет.
/// Тесту нужна работающая база PostgreSQL: если она недоступна, проверка
/// пропускается, чтобы обычный запуск тестов не требовал docker compose.
/// </summary>
public sealed class SaveChangesTests
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5433;Database=doska;Username=doska;Password=doska";

    [Fact]
    public async Task Domain_owns_timestamps_and_persistence_does_not_change_them()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = ResolveConnectionString();

        if (!await DatabaseIsReachableAsync(connectionString, cancellationToken))
        {
            Assert.Skip(
                "PostgreSQL недоступен по адресу из строки подключения: проверка пропущена. " +
                "Для полной проверки запустите 'docker compose up -d postgres' из корня проекта.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        var createdAt = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
        var renamedAt = createdAt.AddMinutes(5);
        var cityName = $"Тестовый город {Guid.NewGuid():N}";
        var updatedCityName = $"{cityName} (обновлён)";

        await using (var context = new AppDbContext(options))
        {
            await context.Database.MigrateAsync(cancellationToken);
            context.Cities.Add(new City(cityName, createdAt));
            await context.SaveChangesAsync(cancellationToken);
        }

        await using (var context = new AppDbContext(options))
        {
            var city = await context.Cities.SingleAsync(
                candidate => candidate.Name == cityName,
                cancellationToken);

            Assert.Equal(createdAt, city.CreatedAt);
            Assert.Equal(createdAt, city.UpdatedAt);

            // Сохранение без доменного изменения отметку не двигает.
            await context.SaveChangesAsync(cancellationToken);
            Assert.Equal(createdAt, city.UpdatedAt);

            city.Rename(updatedCityName, renamedAt);
            await context.SaveChangesAsync(cancellationToken);

            Assert.Equal(createdAt, city.CreatedAt);
            Assert.Equal(renamedAt, city.UpdatedAt);
        }

        await using (var context = new AppDbContext(options))
        {
            var city = await context.Cities.SingleAsync(
                candidate => candidate.Name == updatedCityName,
                cancellationToken);

            Assert.Equal(createdAt, city.CreatedAt);
            Assert.Equal(renamedAt, city.UpdatedAt);

            context.Cities.Remove(city);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        return string.IsNullOrWhiteSpace(fromEnvironment) ? DefaultConnectionString : fromEnvironment;
    }

    private static async Task<bool> DatabaseIsReachableAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var host = string.IsNullOrWhiteSpace(builder.Host) ? "localhost" : builder.Host;
            var port = builder.Port == 0 ? 5432 : builder.Port;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));

            using var client = new TcpClient();
            await client.ConnectAsync(host, port, timeout.Token);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
