using System.Diagnostics;
using Discord.Commands;
using Discord.WebSocket;

namespace PiBot.Discord.Services.Commands;

// https://github.com/discord-net/Discord.Net/issues/1397
// this task.run returns back => scope is disposed with using => have to pass it through context
// https://github.com/discord-net/Discord.Net/blob/de8da0d3b998d32e248e5e438039d266139e4776/src/Discord.Net.Commands/Info/CommandInfo.cs#L229
public class PiBotCommandContext : SocketCommandContext
{
    internal AsyncServiceScope Scope { get; }

    internal Stopwatch Execution { get; }

    public PiBotCommandContext(AsyncServiceScope scope, DiscordSocketClient client, SocketUserMessage msg) : base(client, msg)
    {
        Scope = scope;
        Execution = Stopwatch.StartNew();
    }
}