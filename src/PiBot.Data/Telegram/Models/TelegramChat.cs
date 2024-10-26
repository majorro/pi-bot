using PiBot.Core.Entities;

namespace PiBot.Data.Telegram.Models;

// https://core.telegram.org/bots/api#chat
public class TelegramChat : Community
{
    public long TelegramId { get; init; }
}