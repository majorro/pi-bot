namespace PiBot.Telegram.Services.Birthdays;

internal sealed class BirthdayNotificationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<BirthdayNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ImportMissingBirthdaysAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var utcNow = timeProvider.GetUtcNow();
            await Task.Delay(BirthdayCalendarHelper.GetNextNotificationTime(utcNow) - utcNow, timeProvider, stoppingToken);
            try
            {
                await ImportMissingBirthdaysAsync(stoppingToken);
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<BirthdaySender>().SendTodaysBirthdayMessagesAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Birthday scheduler failed: {ErrorType}", exception.GetType().Name);
            }
        }
    }

    private async Task ImportMissingBirthdaysAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<BirthdayProfileImporter>().ImportMissingBirthdaysAsync();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Birthday profile discovery failed");
        }
    }
}