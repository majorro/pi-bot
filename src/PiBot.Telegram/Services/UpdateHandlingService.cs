using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace PiBot.Telegram.Services;

internal class UpdateHandlingService : IUpdateHandler
{
    private readonly ILogger<UpdateHandlingService> _logger;
    private readonly CommandHandlingService _commandHandlingService;

    private ITelegramBotClient _botClient = default!;

    public UpdateHandlingService(
        ILogger<UpdateHandlingService> logger,
        CommandHandlingService commandHandlingService)
    {
        _logger = logger;
        _commandHandlingService = commandHandlingService;
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken = default)
    {
        _botClient = botClient;

        _logger.LogError("{Exception}", exception);
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
            try
            {
                await _commandHandlingService.HandleCommandAsync(messageText[1..], msg.From!, msg.Chat);
            }
            catch
            {
                await _botClient.SendTextMessageAsync(msg.Chat, "ВСЕ В ДЕРЬМЕ!!!");
                success = false;
                throw;
            }
            finally
            {
                _logger.LogInformation("[{Chat}] [{User}] [{Command}]: {Result}", msg.Chat, msg.From!, messageText, success ? "Success" : "Fail");
            }
        }
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogWarning("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}