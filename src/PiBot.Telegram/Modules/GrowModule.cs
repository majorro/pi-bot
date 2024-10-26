using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PiBot.Core.CommandHandlers;
using PiBot.Data.Telegram;
using PiBot.Data.Telegram.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static PiBot.Telegram.Utils.HtmlFormatHelper;

namespace PiBot.Telegram.Modules;

internal class GrowModule : ModuleBase
{
    public override string CommandName => "grow";
    public override string[] Aliases => ["g"];

    private readonly TelegramPgContext _dbContext;
    private readonly GrowHandler _handler;

    [UsedImplicitly] private GrowModule() : base(null) { }

    public GrowModule(ITelegramBotClient botClient, TelegramPgContext dbContext, GrowHandler handler) : base(botClient)
    {
        _dbContext = dbContext;
        _handler = handler;
    }

    public override async Task<Message> ExecuteAsync(string subCommand = "", params string[] args) =>
        subCommand switch
        {
            _ => await GrowAsync()
        };

    private async Task<Message> GrowAsync()
    {
        var userDto = Context.User;
        var chatDto = Context.Chat;

        // TODO: refactor
        var userChatId = await _dbContext.TelegramUserChats
            .Where(x => x.TelegramUserId == userDto.Id && x.TelegramChatId == chatDto.Id)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync();
        if (userChatId == null)
        {
            var user = await _dbContext.TelegramUsers.SingleOrDefaultAsync(x => x.TelegramId == userDto.Id);
            if (user == null)
            {
                user ??= new TelegramUser
                {
                    TelegramId = userDto.Id,
                    FirstName = userDto.FirstName,
                    LastName = userDto.LastName,
                    Username = userDto.Username,
                };

                _dbContext.TelegramUsers.Add(user);
                await _dbContext.SaveChangesAsync();
            }

            var chat = await _dbContext.TelegramChats.SingleOrDefaultAsync(x => x.TelegramId == chatDto.Id);
            if (chat == null)
            {
                chat ??= new TelegramChat
                {
                    TelegramId = chatDto.Id,
                };

                _dbContext.TelegramChats.Add(chat);
                await _dbContext.SaveChangesAsync();
            }

            var userChat = new TelegramUserChat()
            {
                User = user,
                Community = chat,
            };

            _dbContext.TelegramUserChats.Add(userChat);
            await _dbContext.SaveChangesAsync();

            userChatId = userChat.Id;
        }
        // TODO: refactor

        var result = await _handler.Handle(userChatId.Value);

        var mention = Mention(userDto);
        var message = result?.Growth switch
        {
            null => $"{mention}, сегодня уже был рост штуковины!!!",
            >= 0 => $"{mention}, ваша штуковина выросла на {Bold(result.Growth)} см!!! теперь её размер {Bold(result.Size)} см!!!",
            < 0 => $"{mention}, ваша штуковина уменьшилась на {Bold(Math.Abs(result.Growth))} см!!! теперь её размер {Bold(result.Size)} см!!!",
        };

        return await _botClient.SendTextMessageAsync(chatDto, message, parseMode: ParseMode.Html);
    }
}