using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PiBot.Data.Models;
using PiBot.Data.Utils;

namespace PiBot.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options, IOptions<PgConfig> config) : PgContextBase(options, config)
{
    public virtual DbSet<Megaprikol> Megaprikols { get; init; }
    public virtual DbSet<User> Users { get; init; }
    public virtual DbSet<UserThing> UserThings { get; init; }
    public virtual DbSet<Community> Communities { get; init; }
    public virtual DbSet<UserCommunity> UserCommunities { get; init; }
}
