using System.Reflection;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Commands;
using Discord.WebSocket;

namespace PiBot.Discord.Services.Commands;

public class CommandHandlingService : DiscordClientService
{
    private readonly CommandService _commands;
    private readonly IServiceScopeFactory _scopeFactory;

    // TODO: add to per-guild config
    private const string Prefix = "!";

    public CommandHandlingService(
        DiscordSocketClient client,
        ILogger<CommandHandlingService> logger,
        CommandService commands,
        IServiceScopeFactory scopeFactory) : base(client, logger)
    {
        _commands = commands;
        _scopeFactory = scopeFactory;

        Client.MessageReceived += HandleMessageAsync;
        _commands.CommandExecuted += OnCommandExecutedAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), scope.ServiceProvider);

        await Client.WaitForReadyAsync(stoppingToken);
    }

    private async Task HandleMessageAsync(SocketMessage messageParam)
    {
        if (messageParam is not SocketUserMessage message || messageParam.Author is not { IsBot: false, IsWebhook: false })
            return;
        if (messageParam.Channel is IDMChannel channel)
            await channel.SendMessageAsync("Я могу быть использован только на серверах");


        var argPos = 0;
        if (message.HasStringPrefix(Prefix, ref argPos))
            await HandleCommandAsync(message, argPos);
    }

    private async Task HandleCommandAsync(SocketUserMessage message, int argPos)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        var context = new ScopedSocketCommandContext(scope, Client, message);

        await _commands.ExecuteAsync(context, argPos, scope.ServiceProvider);
    }

    private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult? result)
    {
        if (context is ScopedSocketCommandContext scopedContext)
            await scopedContext.Scope.DisposeAsync();

        if (!string.IsNullOrEmpty(result?.ErrorReason))
        {
            await context.Channel.SendMessageAsync(MentionUtils.MentionUser(473600264805285909));
            await context.Channel.SendMessageAsync("https://media1.tenor.com/m/GWvoxXpr7VsAAAAC/%D0%BC%D1%8B-%D0%B2-%D0%B4%D0%B5%D1%80%D1%8C%D0%BC%D0%B5.gif");
        }

        var commandName = command.IsSpecified
            ? command.Value.Aliases[0]
            : "A command";
        if (result?.IsSuccess ?? false) Logger.LogInformation("[{Guild}] [{Channel}] [{User}] [{Command}]: {Result}", context.Guild, context.Channel, context.User, commandName, result);
        else Logger.LogError("[{Guild}] [{Channel}] [{User}] [{Command}]: {Result}", context.Guild, context.Channel, context.User, commandName, result?.ErrorReason);
    }
}
