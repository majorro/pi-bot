using JetBrains.Annotations;
using PiBot.Core.CommandHandlers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PiBot.Telegram.Modules;

internal class MegaprikolModule : ModuleBase
{
    public override string CommandName => "megaprikol";
    public override string[] Aliases => ["mp"];

    private readonly MegaprikolHandler _handler;

    [UsedImplicitly] private MegaprikolModule() : base(null) { }

    public MegaprikolModule(ITelegramBotClient botClient, MegaprikolHandler handler) : base(botClient)
    {
        _handler = handler;
    }

    public override async Task<Message> ExecuteAsync(string subCommand = "", params string[] args) =>
        subCommand switch
        {
            "unseen" => await GetUnseenCountAsync(),
            _ => await GetMegaprikolAsync(),
        };

    private async Task<Message> GetUnseenCountAsync()
    {
        throw new NotImplementedException();
    }

    private async Task<Message> GetMegaprikolAsync()
    {
        // await _botClient.SendTextMessageAsync(Context.Chat, await _handler.HandleAsync());
        return await BotClient.SendTextMessageAsync(Context.Chat, "мегаприколы допиливаются(");
    }
}