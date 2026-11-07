using PiBot.Core.Entities;

namespace PiBot.Data.Telegram.Models;

// https://core.telegram.org/bots/api#user
public class TelegramUser : User
{
    public long TelegramId { get; init; }

    public string FirstName { get; set; } = default!;
    public string? LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();

    public string? Username { get; set; }

    public int? ManualBirthdayDay { get; set; }
    public int? ManualBirthdayMonth { get; set; }
    public int? ProfileBirthdayDay { get; set; }
    public int? ProfileBirthdayMonth { get; set; }
    public bool BirthdayNotificationsEnabled { get; set; } = true;
}