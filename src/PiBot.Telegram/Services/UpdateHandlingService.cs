using PiBot.Telegram.Modules;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace PiBot.Telegram.Services;

internal class UpdateHandlingService(
    ILogger<UpdateHandlingService> logger,
    CommandHandlingService commandHandlingService)
    : IUpdateHandler
{
    private ITelegramBotClient _botClient = null!;

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken = default)
    {
        _botClient = botClient;

        logger.LogError("{Exception}", exception);
        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken = default)
    {
        _botClient = botClient;
        cancellationToken.ThrowIfCancellationRequested();
        await (update switch
        {
            { Message: { } message } => OnMessage(message),
            _ => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg)
    {
        if (msg.Text is not { } messageText)
            return;

        if (messageText.StartsWith('/'))
        {
            var success = true;
            var ctx = new ModuleContext(msg.From!, msg.Chat);
            try
            {
                await commandHandlingService.HandleCommandAsync(messageText[1..], ctx);
            }
            catch
            {
                await _botClient.SendMessage(msg.Chat, "@majorro228");
                await _botClient.SendMessage(msg.Chat, "https://media1.tenor.com/m/GWvoxXpr7VsAAAAC/%D0%BC%D1%8B-%D0%B2-%D0%B4%D0%B5%D1%80%D1%8C%D0%BC%D0%B5.gif");
                success = false;
                throw;
            }
            finally
            {
                using var _ = logger.BeginScope(ctx.Meta);
                logger.LogInformation("[{Chat}] [{User}] [{Command}]: {Result}", msg.Chat, msg.From!, messageText, success ? "Success" : "Fail");
            }
        }
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogWarning("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}