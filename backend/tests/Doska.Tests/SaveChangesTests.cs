using System.Net.Sockets;
using Doska.Domain.Entities;
using Doska.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Doska.Tests;

/// <summary>
/// Проверка заполнения <c>created_at</c> и <c>updated_at</c> в <c>SaveChanges</c>.
/// Тесту нужна работающая база PostgreSQL: если она недоступна, проверка
/// пропускается, чтобы обычный запуск тестов не требовал docker compose.
/// </summary>
public sealed class SaveChangesTests
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5433;Database=doska;Username=doska;Password=doska";

    [Fact]
    public async Task SaveChanges_fills_created_at_and_updates_updated_at()
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

        var start = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
        var clock = new ManualTimeProvider(start);
        var cityName = $"Тестовый город {Guid.NewGuid():N}";
        var updatedCityName = $"{cityName} (обновлён)";

        await using (var context = new AppDbContext(options, clock))
        {
            await context.Database.MigrateAsync(cancellationToken);
            context.Cities.Add(new City { Name = cityName });
            await context.SaveChangesAsync(cancellationToken);
        }

        await using (var context = new AppDbContext(options, clock))
        {
            var city = await context.Cities.SingleAsync(
                candidate => candidate.Name == cityName,
                cancellationToken);

            Assert.Equal(start, city.CreatedAt);
            Assert.Equal(start, city.UpdatedAt);

            clock.UtcNow = start.AddMinutes(5);
            city.Name = updatedCityName;
            await context.SaveChangesAsync(cancellationToken);

            Assert.Equal(start, city.CreatedAt);
            Assert.Equal(start.AddMinutes(5), city.UpdatedAt);
        }

        await using (var context = new AppDbContext(options, clock))
        {
            var city = await context.Cities.SingleAsync(
                candidate => candidate.Name == updatedCityName,
                cancellationToken);

            Assert.Equal(start.AddMinutes(5), city.UpdatedAt);

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

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
