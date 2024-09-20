using Microsoft.Extensions.Logging;
using PiBot.Core.CommandHandlers.Abstract;

namespace PiBot.Core.CommandHandlers;

public class Leaderboard(ILogger<Leaderboard> logger) : ICommandHandler
{
    public string[] Commands => ["leaderboard", "lb"];

    public Task HandleAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}