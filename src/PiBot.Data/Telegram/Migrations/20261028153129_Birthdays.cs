using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiBot.Data.Telegram.Migrations
{
    /// <inheritdoc />
    public partial class Birthdays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "birthday_notifications_enabled",
                table: "telegram_users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "manual_birthday_day",
                table: "telegram_users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "manual_birthday_month",
                table: "telegram_users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "profile_birthday_day",
                table: "telegram_users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "profile_birthday_month",
                table: "telegram_users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_member",
                table: "telegram_user_chats",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "membership_updated_at",
                table: "telegram_user_chats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_telegram_users_manual_birthday",
                table: "telegram_users",
                sql: "(manual_birthday_day IS NULL AND manual_birthday_month IS NULL) OR (manual_birthday_day IS NOT NULL AND manual_birthday_month IS NOT NULL AND manual_birthday_month BETWEEN 1 AND 12 AND manual_birthday_day BETWEEN 1 AND CASE manual_birthday_month WHEN 2 THEN 29 WHEN 4 THEN 30 WHEN 6 THEN 30 WHEN 9 THEN 30 WHEN 11 THEN 30 ELSE 31 END)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_telegram_users_profile_birthday",
                table: "telegram_users",
                sql: "(profile_birthday_day IS NULL AND profile_birthday_month IS NULL) OR (profile_birthday_day IS NOT NULL AND profile_birthday_month IS NOT NULL AND profile_birthday_month BETWEEN 1 AND 12 AND profile_birthday_day BETWEEN 1 AND CASE profile_birthday_month WHEN 2 THEN 29 WHEN 4 THEN 30 WHEN 6 THEN 30 WHEN 9 THEN 30 WHEN 11 THEN 30 ELSE 31 END)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_telegram_users_manual_birthday",
                table: "telegram_users");

            migrationBuilder.DropCheckConstraint(
                name: "ck_telegram_users_profile_birthday",
                table: "telegram_users");

            migrationBuilder.DropColumn(
                name: "birthday_notifications_enabled",
                table: "telegram_users");

            migrationBuilder.DropColumn(
                name: "manual_birthday_day",
                table: "telegram_users");

            migrationBuilder.DropColumn(
                name: "manual_birthday_month",
                table: "telegram_users");

            migrationBuilder.DropColumn(
                name: "profile_birthday_day",
                table: "telegram_users");

            migrationBuilder.DropColumn(
                name: "profile_birthday_month",
                table: "telegram_users");

            migrationBuilder.DropColumn(
                name: "is_member",
                table: "telegram_user_chats");

            migrationBuilder.DropColumn(
                name: "membership_updated_at",
                table: "telegram_user_chats");
        }
    }
}