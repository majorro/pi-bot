using PiBot.Telegram.Abstract.Polling;
using Telegram.Bot;

namespace PiBot.Telegram.Services.Polling;

public class ReceiverService(
    ITelegramBotClient botClient,
    UpdateHandlers updateHandler,
    ILogger<ReceiverService> logger)
    : ReceiverServiceBase<UpdateHandlers>(botClient, updateHandler, logger);
