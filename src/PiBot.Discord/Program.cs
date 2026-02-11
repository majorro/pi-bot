using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.AspNetCore.Builder;
using PiBot.Core.Configuration;
using PiBot.Core.Contracts;
using PiBot.Data.Common.Extensions;
using PiBot.Data.Discord;
using PiBot.Data.Readonly;
using PiBot.Discord;
using PiBot.Discord.Services;
using PiBot.Discord.Services.Commands;

DotEnv.TryLoad("../../.env"); // for local run

// TODO: fix when https://github.com/DaveSenn/ConfigurationPlaceholders/pull/7 will be merged
// var builder = Host.CreateApplicationBuilder(args);
var builder = WebApplication.CreateBuilder(args)
    .AddConfigurationPlaceholders(new EnvironmentVariableCustomMissingResolver());

if (builder.Environment.IsProduction())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

var botConfigurationSection = builder.Configuration.GetSection(BotConfiguration.SectionName);
if (!botConfigurationSection.Exists())
{
    throw new InvalidOperationException("Bot configuration is missing");
}
builder.Services.Configure<BotConfiguration>(botConfigurationSection);

builder.AddPg<ICommonDbContext, DiscordPgContext, DiscordPgConfig>();
builder.AddPg<IReadonlyDbContext, ReadonlyPgContext, ReadonlyPgConfig>();

builder.AddCore();

builder.Services.AddDiscordHost((conf, sp) =>
    {
        var botConfig = sp.GetConfiguration<BotConfiguration>();

        conf.SocketConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Verbose,
            GatewayIntents = GatewayIntents.All,
            AlwaysDownloadUsers = false,
            MessageCacheSize = 0,
        };
        conf.Token = botConfig.Token;
        conf.LogFormat = (message, exception) => $"({message.Source}) {message.Message} {exception}";
    });

builder.Services.AddCommandService((conf, _) =>
{
    conf.DefaultRunMode = RunMode.Async;
    conf.CaseSensitiveCommands = false;
    conf.LogLevel = LogSeverity.Verbose;
});
// builder.Services.AddInteractionService((conf, _) =>
// {
//     conf.DefaultRunMode = Discord.Interactions.RunMode.Async;
// });

builder.Services.AddHostedService<CommandHandlingService>();
builder.Services.AddHostedService<StateService>();

var host = builder.Build();
host.UsePg<DiscordPgContext, DiscordPgConfig>();
NotifyOnStartup(host);
await host.RunAsync();
return;

// TODO: move to service?
static void NotifyOnStartup(IApplicationBuilder app)
{
    var client = app.ApplicationServices.GetRequiredService<DiscordSocketClient>();
    client.Ready += async () =>
    {
        var channel = await client.GetDMChannelAsync(1291456115111891014);
        await channel.SendMessageAsync("стартуем!");
    };
}

namespace PiBot.Discord
{
    public class BotConfiguration
    {
        public static readonly string SectionName = nameof(BotConfiguration);

        public string Token { get; init; } = default!;
    }
}