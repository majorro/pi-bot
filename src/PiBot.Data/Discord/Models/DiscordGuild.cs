using PiBot.Core.Entities;

namespace PiBot.Data.Discord.Models;

// https://discord.com/developers/docs/resources/guild
public class DiscordGuild : Community
{
    public ulong DiscordId { get; init; }
}