using PiBot.Core.Utils;
using PiBot.Data;
using PiBot.Data.Utils.Extensions;
using PiBot.Telegram;
using PiBot.Telegram.Services;
using PiBot.Telegram.Services.Polling;
using Telegram.Bot;

// TODO: ?
DotEnv.TryLoad("../../.env");

if (Environment.GetEnvironmentVariable("USE_LONGPOLLING") == "true")
{
    // TODO: fix when https://github.com/DaveSenn/ConfigurationPlaceholders/pull/7 will be merged
    var builder = WebApplication.CreateBuilder(args)
        .AddConfigurationPlaceholders(new EnvironmentVariableCustomMissingResolver());
    // var builder = Host.CreateApplicationBuilder(args);

    var botConfigurationSection = builder.Configuration.GetSection(BotConfiguration.Configuration);
    if (!botConfigurationSection.Exists())
    {
        throw new InvalidOperationException("Bot configuration is missing");
    }
    builder.Services.Configure<BotConfiguration>(botConfigurationSection);

    builder.AddPg<DatabaseContext>();

    builder.Services
        .AddHttpClient("telegram_bot_client")
        .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
        {
            var botConfig = sp.GetConfiguration<BotConfiguration>();
            TelegramBotClientOptions options = new(botConfig.Token);
            return new TelegramBotClient(options, httpClient);
        });

    builder.Services
        .AddScoped<UpdateHandlers>()
        .AddScoped<ReceiverService>()
        .AddHostedService<PollingService>();

    var host = builder.Build();
    host.UsePg<DatabaseContext>();
    await host.RunAsync();
}
else
{
    var builder = WebApplication.CreateBuilder(args)
        .AddConfigurationPlaceholders(new EnvironmentVariableCustomMissingResolver());

    var botConfigurationSection = builder.Configuration.GetSection(BotConfiguration.Configuration);
    if (!botConfigurationSection.Exists())
    {
        throw new InvalidOperationException("Bot configuration is missing");
    }
    builder.Services.Configure<BotConfiguration>(botConfigurationSection);

    builder.AddPg<DatabaseContext>();

    builder.Services
        .AddHttpClient("telegram_bot_client")
        .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
        {
            var botConfig = sp.GetConfiguration<BotConfiguration>();
            TelegramBotClientOptions options = new(botConfig.Token);
            return new TelegramBotClient(options, httpClient);
        });

    builder.Services
        .AddScoped<UpdateHandlers>()
        .AddHostedService<ConfigureWebhook>();

    builder.Services
        .AddControllers();

    var app = builder.Build();
    app.UsePg<DatabaseContext>();
    app.MapControllers();
    await app.RunAsync();
}

namespace PiBot.Telegram
{
    public class BotConfiguration
    {
        public static readonly string Configuration = "BotConfiguration";

        public string Token { get; init; } = default!;
        public string HostAddress { get; init; } = default!;
        public string Route { get; init; } = default!;
        public string SecretToken { get; init; } = default!;
    }
}