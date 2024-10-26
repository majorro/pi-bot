using PiBot.Telegram.Abstract.Polling;

namespace PiBot.Telegram.Services.Polling;

internal class PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger)
    : PollingServiceBase<ReceiverService>(serviceProvider, logger);
