using System.Text;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using PiBot.Core.CommandHandlers;
using PiBot.Data.Discord;
using PiBot.Data.Discord.Models;
using PiBot.Discord.Data;

namespace PiBot.Discord.Modules;

[Group("leaderboard")]
[Alias("lb")]
public class LeaderboardModule(DiscordPgContext dbContext, LeaderboardHandler handler) : ModuleBase<SocketCommandContext>
{
    [Command]
    public async Task GetLeaderboardAsync()
    {
        // TODO: refactor
        var guildId = await dbContext.DiscordGuilds
            .Where(x => x.DiscordId == Context.Guild.Id)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync();
        if (guildId == null)
        {
            var guild = Context.Guild.ToModel();
            dbContext.Add(guild);
            await dbContext.SaveChangesAsync();

            guildId = guild.Id;
        }
        // TODO: refactor

        var dataRows = await handler
            .GetQuery(guildId.Value)
            .Select(x => new LeaderboardRowData
            (
                ((DiscordUser)x.UserCommunity.User).DiscordId,
                x.Size
            ))
            .ToArrayAsync();

        var embed = CreateEmbed(dataRows);
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    private Embed CreateEmbed(LeaderboardRowData[] rows)
    {
        var content = new StringBuilder();
        var i = 1;
        foreach (var row in rows)
        {
            var user = MentionUtils.MentionUser(row.DiscordUserId);
            var thingySize = Format.Italics(row.ThingySize.ToString());
            var str = $"{i++}. {user} — {thingySize} см\n";
            content.Append(row.DiscordUserId == Context.User.Id ? Format.Bold(str) : str);
        }

        var embed = new EmbedBuilder();
        embed
            .WithTitle("Топ штуковин 🤯")
            .WithDescription(content.ToString())
            .WithFooter(Context.Guild.Name)
            .WithCurrentTimestamp();

        return embed.Build();
    }

    private record LeaderboardRowData(ulong DiscordUserId, int ThingySize);
}