namespace PiBot.Telegram.Abstract.Polling;

public interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}
