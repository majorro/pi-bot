using System.Diagnostics;
using Discord.Commands;
using Discord.WebSocket;

namespace PiBot.Discord.Services.Commands;

// https://github.com/discord-net/Discord.Net/issues/1397
// this task.run returns back => scope is disposed with using => have to pass it through context
// https://github.com/discord-net/Discord.Net/blob/de8da0d3b998d32e248e5e438039d266139e4776/src/Discord.Net.Commands/Info/CommandInfo.cs#L229
public class PiBotCommandContext(AsyncServiceScope scope, DiscordSocketClient client, SocketUserMessage msg)
    : SocketCommandContext(client, msg)
{
    internal AsyncServiceScope Scope { get; } = scope;

    internal Stopwatch Execution { get; } = Stopwatch.StartNew();
}