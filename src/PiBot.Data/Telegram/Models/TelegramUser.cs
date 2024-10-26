using PiBot.Core.Entities;

namespace PiBot.Data.Telegram.Models;

// https://core.telegram.org/bots/api#user
public class TelegramUser : User
{
    public long TelegramId { get; init; }

    public string FirstName { get; init; } = default!;
    public string? LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}".Trim();

    public string? Username { get; init; }
}
