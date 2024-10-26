using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace PiBot.Data.Common;

public abstract class PgContextBase<TConfig>(DbContextOptions options, IOptions<TConfig> config) : DbContext(options)
    where TConfig : PgConfigBase
{
    private readonly TConfig _config = config.Value;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connString =
            $"Host={_config.Host};Port={_config.Port};Database={_config.DbName};Username={_config.Username};Password={_config.Password}";

        optionsBuilder
            .UseNpgsql(connString)
            .EnableSensitiveDataLogging()
            .UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<DateTime>().HaveConversion<DateTimeAsUtcValueConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableDateTimeAsUtcValueConverter>();
    }

    private class NullableDateTimeAsUtcValueConverter() : ValueConverter<DateTime?, DateTime?>(
        v => !v.HasValue ? v : ToUtc(v.Value), v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
        private static DateTime? ToUtc(DateTime v) => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime();
    }
    private class DateTimeAsUtcValueConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}

public abstract class PgConfigBase
{
    public string Host { get; init; } = default!;
    public string Port { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;

    public virtual string DbName { get; init; } = default!;
}