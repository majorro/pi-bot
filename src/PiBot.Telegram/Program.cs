using PiBot.Core.Configuration;
using PiBot.Core.Contracts;
using PiBot.Data.Common.Extensions;
using PiBot.Data.Readonly;
using PiBot.Data.Telegram;
using PiBot.Telegram;
using PiBot.Telegram.Modules.Configuration;
using PiBot.Telegram.Services;
using PiBot.Telegram.Services.Polling;
using Serilog;
using Serilog.Events;
using Telegram.Bot;
using Telegram.Bot.Types;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

DotEnv.TryLoad("../../.env"); // for local run

// TODO: fix when https://github.com/DaveSenn/ConfigurationPlaceholders/pull/7 will be merged
var builder = WebApplication.CreateBuilder(args)
    .AddConfigurationPlaceholders(new EnvironmentVariableCustomMissingResolver());
// var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog();

var botConfigurationSection = builder.Configuration.GetSection(BotConfiguration.SectionName);
if (!botConfigurationSection.Exists())
{
    throw new InvalidOperationException("Bot configuration is missing");
}
builder.Services.Configure<BotConfiguration>(botConfigurationSection);

builder.AddPg<ICommonDbContext, TelegramPgContext, TelegramPgConfig>();
builder.AddPg<IReadonlyDbContext, ReadonlyPgContext, ReadonlyPgConfig>();

builder.AddCore();

builder.Services
    .AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        var botConfig = sp.GetConfiguration<BotConfiguration>();
        TelegramBotClientOptions options = new(botConfig.Token);
        return new TelegramBotClient(options, httpClient);
    });

builder.Services
    .AddScoped<CommandHandlingService>()
    .AddScoped<UpdateHandlingService>()
    .AddScoped<ReceiverService>()
    .AddHostedService<PollingService>()
    .AddModules();

var host = builder.Build();
host.UsePg<TelegramPgContext, TelegramPgConfig>();
await NotifyOnStartup(host);
await host.RunAsync();
return;

// TODO: move to service?
static async Task NotifyOnStartup(IApplicationBuilder app)
{
    var client = app.ApplicationServices.GetRequiredService<ITelegramBotClient>();
    await client.SendTextMessageAsync(new ChatId(417500081), "стартуем!");
}

namespace PiBot.Telegram
{
    public class BotConfiguration
    {
        public static readonly string SectionName = nameof(BotConfiguration);

        public string Token { get; init; } = default!;
    }
}