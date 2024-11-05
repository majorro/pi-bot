using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PiBot.Data.Discord;
using PiBot.Discord.Data;

namespace PiBot.Discord.Services;

public class StateService : DiscordClientService
{
    private readonly IServiceProvider _serviceProvider;
    private CancellationToken _cancellationToken;

    public StateService(
        DiscordSocketClient client,
        ILogger<StateService> logger,
        IServiceProvider serviceProvider) : base(client, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _cancellationToken = stoppingToken;

        await Client.WaitForReadyAsync(stoppingToken);

        Client.GuildMemberUpdated += OnGuildMemberUpdatedAsync;
    }

    // TODO: now it called for each guild of user if changed some profile info, find some ways to optimise
    private async Task OnGuildMemberUpdatedAsync(Cacheable<SocketGuildUser, ulong> _, SocketGuildUser newUser)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DiscordPgContext>();

        var oldUser = await dbContext.DiscordUsers.SingleOrDefaultAsync(x => x.DiscordId == newUser.Id, _cancellationToken);
        if (oldUser != null)
        {
            oldUser.Map(newUser);
            await dbContext.SaveChangesAsync(_cancellationToken);
        }
    }
}