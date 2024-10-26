using PiBot.Core.Entities;

namespace PiBot.Data.Discord.Models;

// https://discord.com/developers/docs/resources/user
public class DiscordUser : User
{
    public ulong DiscordId { get; init; }

    /// <summary>
    /// nickname/the one overriden on the server
    /// </summary>
    public string DisplayName { get; init; } = default!;

    public string Username { get; init; } = default!;
}
