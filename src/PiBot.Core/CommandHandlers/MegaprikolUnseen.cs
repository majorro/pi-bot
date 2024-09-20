using Microsoft.Extensions.Logging;
using PiBot.Core.CommandHandlers.Abstract;

namespace PiBot.Core.CommandHandlers;

public class MegaprikolUnseen(ILogger<MegaprikolUnseen> logger) : ICommandHandler
{
    public string[] Commands => ["megaprikol_unseen", "mp_unseen"];

    public Task HandleAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}