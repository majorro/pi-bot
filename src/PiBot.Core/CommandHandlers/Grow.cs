using Microsoft.Extensions.Logging;
using PiBot.Core.CommandHandlers.Abstract;

namespace PiBot.Core.CommandHandlers;

public class Grow(ILogger<Grow> logger) : ICommandHandler
{
    public string[] Commands => ["grow", "g"];

    public Task HandleAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

// internal class GrowHandlingService
// {
//     private readonly ILogger<GrowHandlingService> _logger;
//     private readonly GrowDbContext _dbContext;
//
//     public GrowHandlingService(ILogger<GrowHandlingService> logger, GrowDbContext dbContext)
//     {
//         _logger = logger;
//         _dbContext = dbContext;
//     }
//
//     public async Task<int?> HandleAsync(GrowCommandDto ctx)
//     {
//         var thing = await _dbContext.Things.FindAsync(ctx.UserId, ctx.GroupId);
//
//         if (thing == null)
//         {
//             // TODO
//             throw new Exception("Thing not found");
//         }
//
//         var ok = TryUpdateThing(thing, out var growth);
//         if (!ok)
//         {
//             // TODO
//             return null;
//         }
//         await _dbContext.SaveChangesAsync();
//
//         return growth;
//     }
//
//     private bool TryUpdateThing(Thing thing, out int growth)
//     {
//         growth = 0;
//
//         if (thing.LastGrowthAt?.ToUniversalTime().DayOfYear == DateTime.UtcNow.DayOfYear) // TODO: configure pg?
//             return false;
//
//         thing.LastGrowthAt = DateTime.UtcNow;
//         growth = GetThingGrowth();
//         thing.Size += growth;
//
//         return true;
//     }
//
//     private static int GetThingGrowth()
//     {
//         var stdDev = 3.9;
//         var mean = 3.0;
//         if (DateTime.Now.Month == 4 && DateTime.Now.Day == 1) // 😼
//         {
//             stdDev = 13;
//             mean = -12;
//         }
//         return (int)Math.Round(new Normal(mean, stdDev).Sample());
//     }
// }