using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace PiBot.Telegram.Services;

public class ConfigureWebhook(
    ILogger<ConfigureWebhook> logger,
    IServiceProvider serviceProvider,
    IOptions<BotConfiguration> botOptions)
    : IHostedService
{
    private readonly BotConfiguration _botConfig = botOptions.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        var webhookAddress = $"{_botConfig.HostAddress}{_botConfig.Route}";
        logger.LogDebug("Setting webhook: {WebhookAddress}", webhookAddress);
        await botClient.SetWebhookAsync(
            url: webhookAddress,
            allowedUpdates: Array.Empty<UpdateType>(),
            secretToken: _botConfig.SecretToken,
            cancellationToken: cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        logger.LogDebug("Removing webhook");
        await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
    }
}
