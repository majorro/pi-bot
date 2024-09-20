using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace PiBot.Data.Utils;

public abstract class PgContextBase : DbContext
{
    private readonly PgConfig _config;

    protected PgContextBase(DbContextOptions options, IOptions<PgConfig> config) : base(options)
    {
        _config = config.Value;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connString =
            $"Host={_config.Host};Port={_config.Port};Database={_config.Database};Username={_config.Username};Password={_config.Password}";

        optionsBuilder
            .UseNpgsql(connString)
            .EnableSensitiveDataLogging()
            .UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}

public class PgConfig
{
    public string Host { get; init; } = default!;
    public string Port { get; init; } = default!;
    public string Database { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
}