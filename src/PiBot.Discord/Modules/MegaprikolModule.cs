using Discord.Commands;
using PiBot.Core.CommandHandlers;
using PiBot.Discord.Services.Commands;

namespace PiBot.Discord.Modules;

[Group("megaprikol")]
[Alias("mp")]
public class MegaprikolModule(MegaprikolHandler handler) : ModuleBase<PiBotCommandContext>
{
    [Command]
    public async Task MegaprikolAsync()
    {
        // await ReplyAsync(await handler.HandleAsync());
        await ReplyAsync("мегаприколы допиливаются(");
    }
}