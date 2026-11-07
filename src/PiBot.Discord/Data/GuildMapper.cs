using Discord.WebSocket;
using PiBot.Data.Discord.Models;

namespace PiBot.Discord.Data;

internal static class GuildMapper
{
    public static void Map(this DiscordGuild guild, SocketGuild dto)
    {
        guild.Name = dto.Name;
    }

    public static DiscordGuild ToModel(this SocketGuild dto)
    {
        var guild = new DiscordGuild()
        {
            DiscordId = dto.Id,
        };
        guild.Map(dto);

        return guild;
    }
}