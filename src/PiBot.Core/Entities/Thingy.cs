using MathNet.Numerics.Distributions;

namespace PiBot.Core.Entities;

public class Thingy : BaseEntity
{
    public int UserCommunityId { get; init; }
    public UserCommunity UserCommunity { get; init; } = default!;

    public int Size { get; private set; }
    public DateTime? LastGrowthAt { get; private set; }

    public bool TryGrow(out int growth)
    {
        growth = 0;

        if (LastGrowthAt?.ToUniversalTime().DayOfYear == DateTime.UtcNow.DayOfYear)
            return false;

        LastGrowthAt = DateTime.UtcNow;
        growth = GenerateGrowth();
        Size += growth;

        return true;
    }

    private static int GenerateGrowth()
    {
        var stdDev = 3.9;
        var mean = 3.0;
        if (DateTime.Now.Month == 4 && DateTime.Now.Day == 1) // 😼
        {
            stdDev = 13;
            mean = -12;
        }

        return (int)Math.Round(new Normal(mean, stdDev).Sample());
    }
}