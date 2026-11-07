using JetBrains.Annotations;
using PiBot.Core.CommandHandlers;
using Microsoft.EntityFrameworkCore;
using PiBot.Data.Telegram;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static PiBot.Telegram.Utils.HtmlFormatHelper;

namespace PiBot.Telegram.Modules;

internal class GrowModule(ITelegramBotClient botClient, TelegramPgContext dbContext, GrowHandler handler)
    : ModuleBase(botClient)
{
    public override string CommandName => "grow";
    public override string[] Aliases => ["g"];

    // Module discovery constructs modules without dependencies to read command metadata.
    [UsedImplicitly] private GrowModule() : this(null, null, null) { }

    public override async Task<Message> ExecuteAsync(string subCommand = "", params string[] args) =>
        subCommand switch
        {
            _ => await GrowAsync()
        };

    private async Task<Message> GrowAsync()
    {
        var userDto = Context.User;
        var chatDto = Context.Chat;

        var userChatId = await dbContext.TelegramUserChats
            .Where(x => x.TelegramUserId == userDto.Id && x.TelegramChatId == chatDto.Id)
            .Select(x => x.Id)
            .SingleAsync();
        var result = await handler.Handle(userChatId);

        var mention = Mention(userDto);
        var message = result?.Growth switch
        {
            null => $"{mention}, сегодня уже был рост штуковины!!!",
            >= 0 => $"{mention}, ваша штуковина выросла на {Bold(result.Growth)} см!!! теперь её размер {Bold(result.Size)} см!!!",
            < 0 => $"{mention}, ваша штуковина уменьшилась на {Bold(Math.Abs(result.Growth))} см!!! теперь её размер {Bold(result.Size)} см!!!",
        };

        var msg = await BotClient.SendMessage(chatDto, message, parseMode: ParseMode.Html);

        if (result is not null)
        {
            Context.Meta["Growth"] = result.Growth;
            Context.Meta["Size"] = result.Size;
        }

        return msg;
    }
}