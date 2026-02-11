using Discord.WebSocket;
using PiBot.Data.Discord.Models;

namespace PiBot.Discord.Data;

internal static class GuildMapper
{
    public static void Map(this DiscordGuild user, SocketGuild dto) { }
    public static DiscordGuild ToModel(this SocketGuild dto)
    {
        var user = new DiscordGuild()
        {
            DiscordId = dto.Id,
        };
        user.Map(dto);

        return user;
    }
}