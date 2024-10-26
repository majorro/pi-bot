using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PiBot.Core.Entities;
using PiBot.Data.Common;
using PiBot.Data.Telegram.Models;

namespace PiBot.Data.Telegram;

public class TelegramPgContext(DbContextOptions<TelegramPgContext> options, IOptions<TelegramPgConfig> config) : CommonPgContext<TelegramPgConfig>(options, config)
{
    public DbSet<TelegramUser> TelegramUsers { get; init; }
    public DbSet<TelegramChat> TelegramChats { get; init; }
    public DbSet<TelegramUserChat> TelegramUserChats { get; init; }

    public override IQueryable<User> Users => TelegramUsers;
    public override IQueryable<Community> Communities => TelegramChats;
    public override IQueryable<UserCommunity> UserCommunities => TelegramUserChats;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TelegramUser>(entity =>
        {
            entity.HasIndex(e => e.TelegramId).IsUnique();

            entity.Property(e => e.FirstName).HasMaxLength(64);
            entity.Property(e => e.LastName).HasMaxLength(64);
            entity.Property(e => e.Username).HasMaxLength(32);
        });

        modelBuilder.Entity<TelegramChat>(entity =>
        {
            entity.HasIndex(e => e.TelegramId).IsUnique();
        });

        modelBuilder.Entity<TelegramUserChat>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.CommunityId }).IsUnique();
            entity.HasIndex(x => new { x.TelegramUserId, x.TelegramChatId }).IsUnique();

            entity
                .HasOne(x => x.User as TelegramUser)
                .WithMany()
                .HasForeignKey(x => new { x.UserId, x.TelegramUserId })
                .HasPrincipalKey(x => new { x.Id, x.TelegramId })
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(x => x.Community as TelegramChat)
                .WithMany()
                .HasForeignKey(x => new { x.CommunityId, x.TelegramChatId })
                .HasPrincipalKey(x => new { x.Id, x.TelegramId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Thingy>(entity =>
        {
            entity
                .HasOne(e => e.UserCommunity as TelegramUserChat)
                .WithMany()
                .HasForeignKey(e => e.UserCommunityId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class TelegramPgConfig : PgConfigBase
{
    public override string DbName => "telegram";
}