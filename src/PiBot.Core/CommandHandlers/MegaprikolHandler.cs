using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PiBot.Core.Contracts;
using PiBot.Core.Entities;

namespace PiBot.Core.CommandHandlers;

public class MegaprikolHandler(ILogger<MegaprikolHandler> logger, IReadonlyDbContext dbContext)
{
    public async Task<string> HandleAsync(CancellationToken cancellationToken = default)
    {
        var mp = await ChooseRandomAsync(cancellationToken);

        mp.Use();
        await dbContext.SaveChangesAsync(cancellationToken);

        return mp.Content;
    }

    public async Task<int> HandleUnseenAsync(int communityId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async Task<Megaprikol> ChooseRandomAsync(CancellationToken cancellationToken) =>
        await dbContext.Megaprikols
            .OrderBy(x => EF.Functions.Random())
            .FirstAsync(cancellationToken: cancellationToken);
}