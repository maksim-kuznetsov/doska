using Doska.Application.Ads;
using Microsoft.Extensions.DependencyInjection;

namespace Doska.Api.Background;

/// <summary>
/// Периодически архивирует активные объявления старше семи дней. Задача идемпотентна:
/// повторный запуск обрабатывает только те объявления, которые ещё активны.
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
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Приложение останавливается.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Не удалось выполнить автоархив объявлений.");
        }
    }
}