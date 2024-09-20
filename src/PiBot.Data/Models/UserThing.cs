using System.ComponentModel.DataAnnotations;

namespace PiBot.Data.Models;

public class UserThing
{
    [Key]
    public int UserCommunityId { get; init; }
    public UserCommunity UserCommunity { get; init; } = default!;

    public int ThingSize { get; init; }

    public DateTime? LastGrowthAt { get; init; }
}