namespace PiBot.Data.Models;

public class Community
{
    public int Id { get; init; }

    // TODO: add check constraint
    public ulong? TelegramId { get; init; }
    public ulong? DiscordId { get; init; }
}