using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiBot.Data.Discord.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSoftDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discord_user_guilds_discord_guilds_community_id_discord_gui",
                table: "discord_user_guilds");

            migrationBuilder.DropForeignKey(
                name: "fk_discord_user_guilds_discord_users_user_id_discord_user_id",
                table: "discord_user_guilds");

            migrationBuilder.DropForeignKey(
                name: "fk_thingies_discord_user_guilds_user_community_id",
                table: "thingies");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "thingies");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "discord_users");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "discord_user_guilds");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "discord_guilds");

            migrationBuilder.AddForeignKey(
                name: "fk_discord_user_guilds_discord_guilds_community_id_discord_gui",
                table: "discord_user_guilds",
                columns: new[] { "community_id", "discord_guild_id" },
                principalTable: "discord_guilds",
                principalColumns: new[] { "id", "discord_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discord_user_guilds_discord_users_user_id_discord_user_id",
                table: "discord_user_guilds",
                columns: new[] { "user_id", "discord_user_id" },
                principalTable: "discord_users",
                principalColumns: new[] { "id", "discord_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_thingies_discord_user_guilds_user_community_id",
                table: "thingies",
                column: "user_community_id",
                principalTable: "discord_user_guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discord_user_guilds_discord_guilds_community_id_discord_gui",
                table: "discord_user_guilds");

            migrationBuilder.DropForeignKey(
                name: "fk_discord_user_guilds_discord_users_user_id_discord_user_id",
                table: "discord_user_guilds");

            migrationBuilder.DropForeignKey(
                name: "fk_thingies_discord_user_guilds_user_community_id",
                table: "thingies");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "thingies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "discord_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "discord_user_guilds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "discord_guilds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "fk_discord_user_guilds_discord_guilds_community_id_discord_gui",
                table: "discord_user_guilds",
                columns: new[] { "community_id", "discord_guild_id" },
                principalTable: "discord_guilds",
                principalColumns: new[] { "id", "discord_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_discord_user_guilds_discord_users_user_id_discord_user_id",
                table: "discord_user_guilds",
                columns: new[] { "user_id", "discord_user_id" },
                principalTable: "discord_users",
                principalColumns: new[] { "id", "discord_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_thingies_discord_user_guilds_user_community_id",
                table: "thingies",
                column: "user_community_id",
                principalTable: "discord_user_guilds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
