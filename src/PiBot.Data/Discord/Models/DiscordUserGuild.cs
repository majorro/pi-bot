using PiBot.Core.Entities;

namespace PiBot.Data.Discord.Models;

public class DiscordUserGuild : UserCommunity
{
    public ulong DiscordUserId { get; init; }
    public ulong DiscordGuildId { get; init; }
}