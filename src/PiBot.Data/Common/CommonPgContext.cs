using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PiBot.Core.Contracts;
using PiBot.Core.Entities;

namespace PiBot.Data.Common;

public abstract class CommonPgContext<TConfig>(DbContextOptions options, IOptions<TConfig> config) :
    PgContextBase<TConfig>(options, config),
    ICommonDbContext
    where TConfig : PgConfigBase
{
    public abstract IQueryable<User> Users { get; }
    public abstract IQueryable<Community> Communities { get; }
    public abstract IQueryable<UserCommunity> UserCommunities { get; }

    public DbSet<Thingy> Thingies { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<User>();
        modelBuilder.Ignore<Community>();
        modelBuilder.Ignore<UserCommunity>();

        modelBuilder.Entity<Thingy>(entity =>
        {
            entity.HasIndex(e => e.UserCommunityId).IsUnique();
            entity.HasIndex(e => e.Size).IsDescending();
        });
    }
}