namespace PiBot.Core.Entities;

public abstract class UserCommunity : BaseEntity
{
    public int UserId { get; init; }
    public User User { get; init; } = default!;

    public int CommunityId { get; init; }
    public Community Community { get; init; } = default!;
}