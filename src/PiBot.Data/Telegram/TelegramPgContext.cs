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
            entity.Property(e => e.BirthdayNotificationsEnabled).HasDefaultValue(true);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_telegram_users_manual_birthday", CreateBirthdayConstraintSql("manual"));
                table.HasCheckConstraint("ck_telegram_users_profile_birthday", CreateBirthdayConstraintSql("profile"));
            });
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

    private static string CreateBirthdayConstraintSql(string birthdaySource) =>
        $"({birthdaySource}_birthday_day IS NULL AND {birthdaySource}_birthday_month IS NULL) OR " +
        $"({birthdaySource}_birthday_day IS NOT NULL AND {birthdaySource}_birthday_month IS NOT NULL AND " +
        $"{birthdaySource}_birthday_month BETWEEN 1 AND 12 AND {birthdaySource}_birthday_day BETWEEN 1 AND " +
        $"CASE {birthdaySource}_birthday_month WHEN 2 THEN 29 WHEN 4 THEN 30 WHEN 6 THEN 30 WHEN 9 THEN 30 WHEN 11 THEN 30 ELSE 31 END)";
}

public sealed class TelegramPgConfig : PgConfigBase
{
    public override string DbName => "telegram";
}