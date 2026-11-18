using Doska.Application.Ads;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Api.Background;

/// <summary>
/// Периодически выполняет обслуживание объявлений: архивирует активные объявления
/// старше семи дней и отправляет напоминания о скором истечении срока. Задачи
/// идемпотентны: повторный запуск не архивирует уже архивные и не дублирует письма.
/// </summary>
public sealed class AdArchiveBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AdArchiveBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Ads:ArchiveBackgroundService", true))
        {
            return;
        }

        var interval = TimeSpan.FromMinutes(
            configuration.GetValue("Ads:ArchiveIntervalMinutes", 60));

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var adService = scope.ServiceProvider.GetRequiredService<AdService>();

            var archived = await adService.ArchiveExpiredAsync(cancellationToken);

            if (archived > 0)
            {
                logger.LogInformation("Автоархив: переведено в архив объявлений — {Count}.", archived);
            }

            var reminded = await adService.SendExpiryRemindersAsync(cancellationToken);

            if (reminded > 0)
            {
                logger.LogInformation("Напоминания о сроке: отправлено писем — {Count}.", reminded);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Приложение останавливается.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Не удалось выполнить обслуживание объявлений.");
        }
    }
}