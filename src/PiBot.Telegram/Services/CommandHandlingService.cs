using PiBot.Telegram.Modules;
using Telegram.Bot.Types;

namespace PiBot.Telegram.Services;

internal class CommandHandlingService(ILogger<CommandHandlingService> logger, IServiceProvider serviceProvider)
{
    public async Task<Message?> HandleCommandAsync(string command, User user, Chat chat)
    {
        var split = command.Split(new[] {' ', '\n'}, StringSplitOptions.RemoveEmptyEntries); // TODO: add any existing empty symbols
        var commandName = split[0].Split('@')[0];
        var commandArgs = split[1..];

        using var scope = serviceProvider.CreateScope();
        ModuleBase? module;
        try // TODO: cache known commands
        {
            module = scope.ServiceProvider.GetRequiredKeyedService<ModuleBase>(commandName);
        }
        catch (InvalidOperationException e) { return null; }

        module.Context = new ModuleContext(user, chat);

        // TODO: implement subcommands
        return await module.ExecuteAsync(string.Empty, commandArgs);
    }
}
