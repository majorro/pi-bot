using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PiBot.Core.Contracts;
using PiBot.Core.Entities;
using PiBot.Data.Common;

namespace PiBot.Data.Readonly;

// TODO: switch to more appropriate storage
/// <summary>
/// Readonly context for common data
/// Migrations are made by hand
/// </summary>
public class ReadonlyPgContext(DbContextOptions<ReadonlyPgContext> options, IOptions<ReadonlyPgConfig> config) :
    PgContextBase<ReadonlyPgConfig>(options, config),
    IReadonlyDbContext
{
    public DbSet<Megaprikol> Megaprikols { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Megaprikol>(entity =>
        {
            entity.Property(x => x.Content).HasMaxLength(10000);
            entity.Property(x => x.UsageCount).HasDefaultValue(0);
        });
    }
}

public sealed class ReadonlyPgConfig : PgConfigBase
{
    public override string DbName => "readonly";
}