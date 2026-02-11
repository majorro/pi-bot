using JetBrains.Annotations;
using PiBot.Core.CommandHandlers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PiBot.Telegram.Modules;

internal class MegaprikolModule(ITelegramBotClient botClient, MegaprikolHandler handler) : ModuleBase(botClient)
{
    public override string CommandName => "megaprikol";
    public override string[] Aliases => ["mp"];

    private readonly MegaprikolHandler _handler = handler;

    [UsedImplicitly] private MegaprikolModule() : this(null, null) { }

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
        return await BotClient.SendMessage(Context.Chat, "мегаприколы допиливаются(");
    }
}