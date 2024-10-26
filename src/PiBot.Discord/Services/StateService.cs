using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;

namespace PiBot.Discord.Services;

// TODO: implement if needed
public class StateService : DiscordClientService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public StateService(
        DiscordSocketClient client,
        ILogger<DiscordClientService> logger,
        IServiceScopeFactory scopeFactory) : base(client, logger)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Client.WaitForReadyAsync(stoppingToken);

        await UpdateStateAsync(stoppingToken);
    }

    private async Task UpdateStateAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async Task HandleServerJoin(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async Task HandleUserServerJoin(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}