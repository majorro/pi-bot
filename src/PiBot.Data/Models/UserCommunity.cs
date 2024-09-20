namespace PiBot.Data.Models;

public class UserCommunity
{
    public int Id { get; init; }

    public int UserId { get; init; }
    public User User { get; init; } = default!;

    public int CommunityId { get; init; }
    public Community Community { get; init; } = default!;
}