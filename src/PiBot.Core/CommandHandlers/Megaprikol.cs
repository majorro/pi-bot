using Microsoft.Extensions.Logging;
using PiBot.Core.CommandHandlers.Abstract;

namespace PiBot.Core.CommandHandlers;

public class Megaprikol(ILogger<Megaprikol> logger) : ICommandHandler
{
    public string[] Commands => ["megaprikol", "mp"];

    public Task HandleAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}