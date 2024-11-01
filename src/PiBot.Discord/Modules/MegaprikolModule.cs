using Discord.Commands;
using PiBot.Core.CommandHandlers;

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