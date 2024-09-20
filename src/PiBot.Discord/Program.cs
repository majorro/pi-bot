using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.AspNetCore.Builder;
using PiBot.Core.Utils;
using PiBot.Data;
using PiBot.Data.Utils.Extensions;
using PiBot.Discord;

// TODO: ?
DotEnv.TryLoad("../../.env");

// TODO: fix when https://github.com/DaveSenn/ConfigurationPlaceholders/pull/7 will be merged
// var builder = Host.CreateApplicationBuilder(args);
var builder = WebApplication.CreateBuilder(args)
    .AddConfigurationPlaceholders(new EnvironmentVariableCustomMissingResolver());

var botConfigurationSection = builder.Configuration.GetSection(BotConfiguration.Configuration);
if (!botConfigurationSection.Exists())
{
    throw new InvalidOperationException("Bot configuration is missing");
}
builder.Services.Configure<BotConfiguration>(botConfigurationSection);

builder.AddPg<DatabaseContext>();

builder.Services.AddDiscordHost((conf, sp) =>
    {
        var botConfig = sp.GetConfiguration<BotConfiguration>();

        conf.SocketConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Verbose,
            GatewayIntents = GatewayIntents.All,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 200
        };
        conf.Token = botConfig.Token;
        conf.LogFormat = (message, exception) => $"({message.Source}) {message.Message} {exception}";
    });

builder.Services.AddCommandService((conf, _) =>
{
    conf.DefaultRunMode = RunMode.Async;
    conf.CaseSensitiveCommands = false;
});
// builder.Services.AddInteractionService((conf, _) =>
// {
//     conf.DefaultRunMode = Discord.Interactions.RunMode.Async;
// });

var host = builder.Build();
host.UsePg<DatabaseContext>();
await host.RunAsync();


namespace PiBot.Discord
{
    public class BotConfiguration
    {
        public static readonly string Configuration = "BotConfiguration";

        public string Token { get; init; } = default!;
    }
}