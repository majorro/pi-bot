using PiBot.Telegram.Abstract.Polling;
using Telegram.Bot;

namespace PiBot.Telegram.Services.Polling;

internal class ReceiverService(
    ITelegramBotClient botClient,
    UpdateHandlingService updateHandlingService,
    ILogger<ReceiverServiceBase<UpdateHandlingService>> logger)
    : ReceiverServiceBase<UpdateHandlingService>(botClient, updateHandlingService, logger);