using PiBot.Core.Entities;

namespace PiBot.Data.Telegram.Models;

public class TelegramUserChat : UserCommunity
{
    public long TelegramUserId { get; init; }
    public long TelegramChatId { get; init; }

    public bool? IsMember { get; set; }
    public DateTime? MembershipUpdatedAt { get; set; }
}