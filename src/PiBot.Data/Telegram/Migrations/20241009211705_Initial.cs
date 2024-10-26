using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PiBot.Data.Telegram.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telegram_chats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    telegram_id = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_chats", x => x.id);
                    table.UniqueConstraint("ak_telegram_chats_id_telegram_id", x => new { x.id, x.telegram_id });
                });

            migrationBuilder.CreateTable(
                name: "telegram_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    telegram_id = table.Column<long>(type: "bigint", nullable: false),
                    first_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    last_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_users", x => x.id);
                    table.UniqueConstraint("ak_telegram_users_id_telegram_id", x => new { x.id, x.telegram_id });
                });

            migrationBuilder.CreateTable(
                name: "telegram_user_chats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    telegram_user_id = table.Column<long>(type: "bigint", nullable: false),
                    telegram_chat_id = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    community_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_user_chats", x => x.id);
                    table.ForeignKey(
                        name: "fk_telegram_user_chats_telegram_chats_community_id_telegram_ch",
                        columns: x => new { x.community_id, x.telegram_chat_id },
                        principalTable: "telegram_chats",
                        principalColumns: new[] { "id", "telegram_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_telegram_user_chats_telegram_users_user_id_telegram_user_id",
                        columns: x => new { x.user_id, x.telegram_user_id },
                        principalTable: "telegram_users",
                        principalColumns: new[] { "id", "telegram_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "thingies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_community_id = table.Column<int>(type: "integer", nullable: false),
                    size = table.Column<int>(type: "integer", nullable: false),
                    last_growth_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_thingies", x => x.id);
                    table.ForeignKey(
                        name: "fk_thingies_telegram_user_chats_user_community_id",
                        column: x => x.user_community_id,
                        principalTable: "telegram_user_chats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_telegram_chats_telegram_id",
                table: "telegram_chats",
                column: "telegram_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_user_chats_community_id_telegram_chat_id",
                table: "telegram_user_chats",
                columns: new[] { "community_id", "telegram_chat_id" });

            migrationBuilder.CreateIndex(
                name: "ix_telegram_user_chats_telegram_user_id_telegram_chat_id",
                table: "telegram_user_chats",
                columns: new[] { "telegram_user_id", "telegram_chat_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_user_chats_user_id_community_id",
                table: "telegram_user_chats",
                columns: new[] { "user_id", "community_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_user_chats_user_id_telegram_user_id",
                table: "telegram_user_chats",
                columns: new[] { "user_id", "telegram_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_telegram_users_telegram_id",
                table: "telegram_users",
                column: "telegram_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_thingies_size",
                table: "thingies",
                column: "size",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_thingies_user_community_id",
                table: "thingies",
                column: "user_community_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "thingies");

            migrationBuilder.DropTable(
                name: "telegram_user_chats");

            migrationBuilder.DropTable(
                name: "telegram_chats");

            migrationBuilder.DropTable(
                name: "telegram_users");
        }
    }
}
