using Microsoft.Extensions.DependencyInjection;

namespace Fauzora.Services;

public sealed class OrderAutoCompletionService(
    IServiceScopeFactory scopeFactory,
    ILogger<OrderAutoCompletionService> logger) : BackgroundService
{
    // Worker berjalan saat startup lalu setiap jam untuk menyelesaikan Delivered yang sudah lewat 7 hari.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await CompleteExpiredOrdersAsync(stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await CompleteExpiredOrdersAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task CompleteExpiredOrdersAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<OrderService>();
            var count = await service.CompleteExpiredDeliveredAsync();
            if (count > 0) logger.LogInformation("Automatically completed {Count} delivered orders.", count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to auto-complete delivered orders.");
        }
    }
}