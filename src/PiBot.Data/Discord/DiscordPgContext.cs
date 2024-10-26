using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PiBot.Core.Entities;
using PiBot.Data.Common;
using PiBot.Data.Discord.Models;

namespace PiBot.Data.Discord;

public class DiscordPgContext(DbContextOptions<DiscordPgContext> options, IOptions<DiscordPgConfig> config) : CommonPgContext<DiscordPgConfig>(options, config)
{
    public DbSet<DiscordUser> DiscordUsers { get; init; }
    public DbSet<DiscordGuild> DiscordGuilds { get; init; }
    public DbSet<DiscordUserGuild> DiscordUserGuilds { get; init; }

    public override IQueryable<User> Users => DiscordUsers;
    public override IQueryable<Community> Communities => DiscordGuilds;
    public override IQueryable<UserCommunity> UserCommunities => DiscordUserGuilds;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DiscordUser>(entity =>
        {
            entity.HasIndex(e => e.DiscordId).IsUnique();

            entity.Property(e => e.DisplayName).HasMaxLength(32);
            entity.Property(e => e.Username).HasMaxLength(32);
        });

        modelBuilder.Entity<DiscordGuild>(entity =>
        {
            entity.HasIndex(e => e.DiscordId).IsUnique();
        });

        modelBuilder.Entity<DiscordUserGuild>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.CommunityId }).IsUnique();
            entity.HasIndex(x => new { x.DiscordUserId, x.DiscordGuildId }).IsUnique();

            entity
                .HasOne(x => x.User as DiscordUser)
                .WithMany()
                .HasForeignKey(x => new { x.UserId, x.DiscordUserId })
                .HasPrincipalKey(x => new { x.Id, x.DiscordId })
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(x => x.Community as DiscordGuild)
                .WithMany()
                .HasForeignKey(x => new { x.CommunityId, x.DiscordGuildId })
                .HasPrincipalKey(x => new { x.Id, x.DiscordId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Thingy>(entity =>
        {
            entity
                .HasOne(e => e.UserCommunity as DiscordUserGuild)
                .WithMany()
                .HasForeignKey(e => e.UserCommunityId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class DiscordPgConfig : PgConfigBase
{
    public override string DbName => "discord";
}