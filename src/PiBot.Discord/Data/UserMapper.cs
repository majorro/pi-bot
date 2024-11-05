using Discord.WebSocket;
using PiBot.Data.Discord.Models;

namespace PiBot.Discord.Data;

internal static class UserMapper
{
    public static void Map(this DiscordUser user, SocketUser dto)
    {
        user.DisplayName = dto.GlobalName ?? user.DisplayName;
        user.Username = dto.Username;
    }
    public static DiscordUser ToModel(this SocketUser dto)
    {
        var user = new DiscordUser()
        {
            DiscordId = dto.Id,
        };
        user.Map(dto);

        return user;
    }
}