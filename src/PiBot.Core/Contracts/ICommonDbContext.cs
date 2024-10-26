using Microsoft.EntityFrameworkCore;
using PiBot.Core.Entities;

namespace PiBot.Core.Contracts;

public interface ICommonDbContext
{
    #region messenger mapped entities

    IQueryable<User> Users { get; }
    IQueryable<Community> Communities { get; }
    IQueryable<UserCommunity> UserCommunities { get; }

    #endregion messenger mapped entities

    DbSet<Thingy> Thingies { get; init; }

    /// <inheritdoc cref="DbContext.SaveChangesAsync(CancellationToken)"/>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}