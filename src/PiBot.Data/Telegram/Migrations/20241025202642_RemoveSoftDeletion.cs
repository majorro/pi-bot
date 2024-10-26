using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiBot.Data.Telegram.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSoftDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_telegram_user_chats_telegram_chats_community_id_telegram_ch",
                table: "telegram_user_chats");

            migrationBuilder.DropForeignKey(
                name: "fk_telegram_user_chats_telegram_users_user_id_telegram_user_id",
                table: "telegram_user_chats");

            migrationBuilder.DropForeignKey(
                name: "fk_thingies_telegram_user_chats_user_community_id",
                table: "thingies");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "thingies");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "telegram_users");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "telegram_user_chats");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "telegram_chats");

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_user_chats_telegram_chats_community_id_telegram_ch",
                table: "telegram_user_chats",
                columns: new[] { "community_id", "telegram_chat_id" },
                principalTable: "telegram_chats",
                principalColumns: new[] { "id", "telegram_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_user_chats_telegram_users_user_id_telegram_user_id",
                table: "telegram_user_chats",
                columns: new[] { "user_id", "telegram_user_id" },
                principalTable: "telegram_users",
                principalColumns: new[] { "id", "telegram_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_thingies_telegram_user_chats_user_community_id",
                table: "thingies",
                column: "user_community_id",
                principalTable: "telegram_user_chats",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_telegram_user_chats_telegram_chats_community_id_telegram_ch",
                table: "telegram_user_chats");

            migrationBuilder.DropForeignKey(
                name: "fk_telegram_user_chats_telegram_users_user_id_telegram_user_id",
                table: "telegram_user_chats");

            migrationBuilder.DropForeignKey(
                name: "fk_thingies_telegram_user_chats_user_community_id",
                table: "thingies");

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "thingies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "telegram_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "telegram_user_chats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "telegram_chats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_user_chats_telegram_chats_community_id_telegram_ch",
                table: "telegram_user_chats",
                columns: new[] { "community_id", "telegram_chat_id" },
                principalTable: "telegram_chats",
                principalColumns: new[] { "id", "telegram_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_user_chats_telegram_users_user_id_telegram_user_id",
                table: "telegram_user_chats",
                columns: new[] { "user_id", "telegram_user_id" },
                principalTable: "telegram_users",
                principalColumns: new[] { "id", "telegram_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_thingies_telegram_user_chats_user_community_id",
                table: "thingies",
                column: "user_community_id",
                principalTable: "telegram_user_chats",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
