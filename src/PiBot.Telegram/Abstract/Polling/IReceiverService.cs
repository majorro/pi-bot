namespace PiBot.Telegram.Abstract.Polling;

internal interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}
