using System.Text.Json;
using PiBot.Core.Configuration;
using PiBot.Core.Contracts;
using PiBot.Data.Common.Extensions;
using PiBot.Data.Readonly;
using PiBot.Data.Telegram;
using PiBot.Telegram;
using PiBot.Telegram.Modules.Configuration;
using PiBot.Telegram.Services;
using PiBot.Telegram.Services.Birthdays;
using PiBot.Telegram.Services.Polling;
using Telegram.Bot;
using Telegram.Bot.Types;

DotEnv.TryLoad("../../.env"); // for local run

// TODO: remove ConfigurationPlaceholders
var builder = WebApplication.CreateBuilder(args)
    .AddConfigurationPlaceholders(new EnvironmentVariableCustomMissingResolver());
// var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(x =>
{
    x.IncludeScopes = true;
    if (!builder.Environment.IsProduction())
        x.JsonWriterOptions = new JsonWriterOptions() { Indented = true };
});

var botConfigurationSection = builder.Configuration.GetSection(BotConfiguration.SectionName);
if (!botConfigurationSection.Exists())
{
    throw new InvalidOperationException("Bot configuration is missing");
}
builder.Services.Configure<BotConfiguration>(botConfigurationSection);
builder.Services.Configure<BirthdayConfiguration>(builder.Configuration.GetSection(nameof(BirthdayConfiguration)));
builder.Services.AddSingleton(TimeProvider.System);

builder.AddPg<ICommonDbContext, TelegramPgContext, TelegramPgConfig>();
builder.AddPg<IReadonlyDbContext, ReadonlyPgContext, ReadonlyPgConfig>();

builder.AddCore();

builder.Services
    .AddHttpClient("TelegramBotClient")
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
    .AddScoped<TelegramMembershipService>()
    .AddScoped<BirthdayProfileImporter>()
    .AddScoped<BirthdayTargetResolver>()
    .AddScoped<BirthdaySender>()
    .AddHostedService<PollingService>()
    .AddHostedService<BirthdayNotificationWorker>()
    .AddModules();

var host = builder.Build();
host.UsePg<TelegramPgContext, TelegramPgConfig>();
await RefreshTelegramUsers(host);
await NotifyOnStartup(host);
await host.RunAsync();
return;

// TODO: move to service?
static async Task NotifyOnStartup(IApplicationBuilder app)
{
    var client = app.ApplicationServices.GetRequiredService<ITelegramBotClient>();
    await client.SendMessage(new ChatId(417500081), "стартуем!");
}

static async Task RefreshTelegramUsers(IApplicationBuilder app)
{
    await using var scope = app.ApplicationServices.CreateAsyncScope();
    var membershipService = scope.ServiceProvider.GetRequiredService<TelegramMembershipService>();
    await membershipService.RefreshKnownUsersAndMembershipsAsync();
}

namespace PiBot.Telegram
{
    public class BotConfiguration
    {
        public static readonly string SectionName = nameof(BotConfiguration);

        public string Token { get; init; } = default!;
        public long[] AdminUserIds { get; init; } = [];
    }
}