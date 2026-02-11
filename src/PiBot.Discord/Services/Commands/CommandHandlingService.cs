using System.Diagnostics;
using System.Reflection;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Commands;
using Discord.WebSocket;

namespace PiBot.Discord.Services.Commands;

public class CommandHandlingService(
    DiscordSocketClient client,
    ILogger<CommandHandlingService> logger,
    CommandService commands,
    IServiceScopeFactory scopeFactory)
    : DiscordClientService(client, logger)
{
    // TODO: add to per-guild config
    private const string Prefix = "!";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.Log += LogAsync;

        Client.MessageReceived += HandleMessageAsync;
        commands.CommandExecuted += OnCommandExecutedAsync;

        await using var scope = scopeFactory.CreateAsyncScope();
        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), scope.ServiceProvider);

        await Client.WaitForReadyAsync(stoppingToken);
    }

    private Task LogAsync(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };

        using (Logger.BeginScope(new Dictionary<string, object>
               {
                   ["DiscordSource"] = message.Source
               }))
        {
            Logger.Log(
                level,
                message.Exception,
                "{Message}", // TODO: is it ok?
                message.Message);
        }

        return Task.CompletedTask;
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
        var scope = scopeFactory.CreateAsyncScope();
        var context = new PiBotCommandContext(scope, Client, message);

        await commands.ExecuteAsync(context, argPos, scope.ServiceProvider);
    }

    private async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult? result)
    {
        var elapsed = 0;
        IDisposable? loggerScope = null;
        if (context is PiBotCommandContext scopedContext)
        {
            scopedContext.Execution.Stop();
            elapsed = (int)scopedContext.Execution.Elapsed.TotalMilliseconds;

            await scopedContext.Scope.DisposeAsync();

            loggerScope = Logger.BeginScope(scopedContext.Meta);
        }

        if (!string.IsNullOrEmpty(result?.ErrorReason) && result.Error != CommandError.UnknownCommand)
        {
            await context.Channel.SendMessageAsync(MentionUtils.MentionUser(473600264805285909));
            await context.Channel.SendMessageAsync("https://media1.tenor.com/m/GWvoxXpr7VsAAAAC/%D0%BC%D1%8B-%D0%B2-%D0%B4%D0%B5%D1%80%D1%8C%D0%BC%D0%B5.gif");
        }

        var commandName = command.IsSpecified
            ? command.Value.Aliases[0]
            : "Unknown";
        if (result?.IsSuccess ?? false)
            Logger.LogInformation(
                "[{Guild}] [{Channel}] [{User}] [{Command}]: {Result} in {DurationMs}ms",
                context.Guild, context.Channel, context.User, commandName, result, elapsed);
        else
            Logger.LogError(
                "[{Guild}] [{Channel}] [{User}] [{Command}]: {Result} in {DurationMs}ms",
                context.Guild, context.Channel, context.User, commandName, result?.ErrorReason, elapsed);

        loggerScope?.Dispose();
    }
}
