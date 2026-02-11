using PiBot.Core.Entities;

namespace PiBot.Data.Discord.Models;

// https://discord.com/developers/docs/resources/user
public class DiscordUser : User
{
    public ulong DiscordId { get; init; }

    public string DisplayName { get; set; } = default!;

    public string Username { get; set; } = default!;
}
