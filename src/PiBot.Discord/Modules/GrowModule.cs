using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PiBot.Core.CommandHandlers;
using PiBot.Data.Discord;
using PiBot.Data.Discord.Models;
using PiBot.Discord.Data;
using PiBot.Discord.Services.Commands;

namespace PiBot.Discord.Modules;

[Group("grow")]
[Alias("g")]
public class GrowModule(DiscordPgContext dbContext, GrowHandler handler) : ModuleBase<PiBotCommandContext>
{
    [Command]
    public async Task GrowAsync()
    {
        var userDto = (SocketGuildUser)Context.User;

        // TODO: refactor
        var userGuildId = await dbContext.DiscordUserGuilds
            .Where(x => x.DiscordUserId == userDto.Id && x.DiscordGuildId == userDto.Guild.Id)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync();
        if (userGuildId == null)
        {
            var user = await dbContext.DiscordUsers.SingleOrDefaultAsync(x => x.DiscordId == userDto.Id);
            if (user == null)
            {
                user = userDto.ToModel();
                dbContext.DiscordUsers.Add(user);
                await dbContext.SaveChangesAsync();
            }

            var guild = await dbContext.DiscordGuilds.SingleOrDefaultAsync(x => x.DiscordId == userDto.Guild.Id);
            if (guild == null)
            {
                guild = userDto.Guild.ToModel();
                dbContext.DiscordGuilds.Add(guild);
                await dbContext.SaveChangesAsync();
            }

            var userGuild = new DiscordUserGuild()
            {
                User = user,
                Community = guild,
            };
            dbContext.DiscordUserGuilds.Add(userGuild);
            await dbContext.SaveChangesAsync();

            userGuildId = userGuild.Id;
        }
        // TODO: refactor

        var result = await handler.Handle(userGuildId.Value);

        var mention = MentionUtils.MentionUser(userDto.Id);
        var message = result?.Growth switch
        {
            null => $"{mention}, сегодня уже был рост штуковины!!!",
            >= 0 => $"{mention}, ваша штуковина выросла на {Format.Bold(result.Growth.ToString())} см!!! теперь её размер {Format.Bold(result.Size.ToString())} см!!!",
            < 0 => $"{mention}, ваша штуковина уменьшилась на {Format.Bold(Math.Abs(result.Growth).ToString())} см!!! теперь её размер {Format.Bold(result.Size.ToString())} см!!!",
        };

        await ReplyAsync(message);
    }
}