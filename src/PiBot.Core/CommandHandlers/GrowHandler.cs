using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PiBot.Core.Contracts;
using PiBot.Core.Entities;

namespace PiBot.Core.CommandHandlers;

public record GrowResult(int Growth, int Size);

public class GrowHandler(ILogger<GrowHandler> logger, ICommonDbContext dbContext)
{
    public async Task<GrowResult?> Handle(int userCommunityId, CancellationToken cancellationToken = default)
    {
        var thingy = await dbContext.Thingies.SingleOrDefaultAsync(x => x.UserCommunityId == userCommunityId, cancellationToken);

        if (thingy == null)
        {
            thingy ??= new Thingy
            {
                UserCommunityId = userCommunityId,
            };

            dbContext.Thingies.Add(thingy);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var grown = thingy.TryGrow(out var growth);
        if (!grown)
        {
            return null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new GrowResult(growth, thingy.Size);
    }
}