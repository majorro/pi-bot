using Microsoft.Extensions.Logging;
using PiBot.Core.Contracts;
using PiBot.Core.Entities;

namespace PiBot.Core.CommandHandlers;

public class LeaderboardHandler(ILogger<LeaderboardHandler> logger, ICommonDbContext dbContext)
{
    private const int DefaultPageSize = 25;

    public IQueryable<Thingy> GetQuery(int communityId, int page = 0) =>
        dbContext.Thingies
            .Where(t => t.UserCommunity.CommunityId == communityId)
            .OrderByDescending(r => r.Size)
            .Skip(DefaultPageSize * page)
            .Take(DefaultPageSize);
}