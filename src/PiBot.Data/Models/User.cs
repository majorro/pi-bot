namespace PiBot.Data.Models;

// TODO: split into tg and dc users?
public class User
{
    public int Id { get; init; }

    // TODO: add check constraint
    public ulong? TelegramId { get; init; }
    public ulong? DiscordId { get; init; }

    public string Nickname { get; init; }

    public string? Username { get; init; }
}
