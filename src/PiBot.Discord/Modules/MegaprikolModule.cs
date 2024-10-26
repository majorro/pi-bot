using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PiBot.Core.CommandHandlers;
using PiBot.Data.Discord;
using PiBot.Data.Discord.Models;

namespace PiBot.Discord.Modules;

[Group("megaprikol")]
[Alias("mp")]
public class MegaprikolModule(MegaprikolHandler handler) : ModuleBase<SocketCommandContext>
{
    [Command]
    public async Task MegaprikolAsync()
    {
        // await ReplyAsync(await handler.HandleAsync());
        await ReplyAsync("мегаприколы допиливаются(");
    }
}