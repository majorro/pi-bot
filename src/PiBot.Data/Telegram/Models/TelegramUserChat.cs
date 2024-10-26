using PiBot.Core.Entities;

namespace PiBot.Data.Telegram.Models;

public class TelegramUserChat : UserCommunity
{
    public long TelegramUserId { get; init; }
    public long TelegramChatId { get; init; }
}