using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PiBot.Core.CommandHandlers;
using PiBot.Data.Telegram.Models;
using PiBot.Data.Telegram;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static PiBot.Telegram.Utils.HtmlFormatHelper;

namespace PiBot.Telegram.Modules;

internal class LeaderboardModule : ModuleBase
{
    public override string CommandName => "leaderboard";
    public override string[] Aliases => ["lb"];

    private readonly TelegramPgContext _dbContext;
    private readonly LeaderboardHandler _handler;

    [UsedImplicitly] private LeaderboardModule() : base(null) { }

    public LeaderboardModule(ITelegramBotClient botClient, TelegramPgContext dbContext, LeaderboardHandler handler) : base(botClient)
    {
        _dbContext = dbContext;
        _handler = handler;
    }
    
    public override async Task<Message> ExecuteAsync(string subCommand = "", params string[] args) =>
        subCommand switch
        {
            _ => await GetLeaderboardAsync()
        };

    private async Task<Message> GetLeaderboardAsync()
    {
        // TODO: refactor
        var chatId = await _dbContext.TelegramChats
            .Where(x => x.TelegramId == Context.Chat.Id)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync();
        if (chatId == null)
        {
            var chat = new TelegramChat
            {
                TelegramId = Context.Chat.Id,
            };

            _dbContext.Add(chat);
            await _dbContext.SaveChangesAsync();

            chatId = chat.Id;
        }
        // TODO: refactor

        var dataRows = await _handler
            .GetQuery(chatId.Value)
            .Select(x => new LeaderboardRowData
            (
                (TelegramUser)x.UserCommunity.User,
                x.Size
            ))
            .AsNoTracking()
            .ToArrayAsync();

        var message = ComposeMessage(dataRows);
        return await _botClient.SendTextMessageAsync(Context.Chat, message, parseMode: ParseMode.Html);
    }

    private string ComposeMessage(LeaderboardRowData[] rows)
    {
        var builder = new StringBuilder();
        builder.Append(Bold("Топ штуковин") + "🤯\n\n");

        var i = 1;
        foreach (var row in rows)
        {
            var user = Mention(row.User);
            var thingySize = Italics(row.ThingySize);
            var str = $"{i++}. {user} — {thingySize} см\n";
            builder.Append(row.User.TelegramId == Context.User.Id ? Bold(str) : str);
        }

        return builder.ToString();
    }

    private record LeaderboardRowData(TelegramUser User, int ThingySize);
}