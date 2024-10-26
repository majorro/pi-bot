using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PiBot.Data.Discord.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "discord_guilds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    discord_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discord_guilds", x => x.id);
                    table.UniqueConstraint("ak_discord_guilds_id_discord_id", x => new { x.id, x.discord_id });
                });

            migrationBuilder.CreateTable(
                name: "discord_users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    discord_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    display_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discord_users", x => x.id);
                    table.UniqueConstraint("ak_discord_users_id_discord_id", x => new { x.id, x.discord_id });
                });

            migrationBuilder.CreateTable(
                name: "discord_user_guilds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    discord_user_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    discord_guild_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    community_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discord_user_guilds", x => x.id);
                    table.ForeignKey(
                        name: "fk_discord_user_guilds_discord_guilds_community_id_discord_gui",
                        columns: x => new { x.community_id, x.discord_guild_id },
                        principalTable: "discord_guilds",
                        principalColumns: new[] { "id", "discord_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_discord_user_guilds_discord_users_user_id_discord_user_id",
                        columns: x => new { x.user_id, x.discord_user_id },
                        principalTable: "discord_users",
                        principalColumns: new[] { "id", "discord_id" },
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
                        name: "fk_thingies_discord_user_guilds_user_community_id",
                        column: x => x.user_community_id,
                        principalTable: "discord_user_guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_discord_guilds_discord_id",
                table: "discord_guilds",
                column: "discord_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discord_user_guilds_community_id_discord_guild_id",
                table: "discord_user_guilds",
                columns: new[] { "community_id", "discord_guild_id" });

            migrationBuilder.CreateIndex(
                name: "ix_discord_user_guilds_discord_user_id_discord_guild_id",
                table: "discord_user_guilds",
                columns: new[] { "discord_user_id", "discord_guild_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discord_user_guilds_user_id_community_id",
                table: "discord_user_guilds",
                columns: new[] { "user_id", "community_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discord_user_guilds_user_id_discord_user_id",
                table: "discord_user_guilds",
                columns: new[] { "user_id", "discord_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_discord_users_discord_id",
                table: "discord_users",
                column: "discord_id",
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
                name: "discord_user_guilds");

            migrationBuilder.DropTable(
                name: "discord_guilds");

            migrationBuilder.DropTable(
                name: "discord_users");
        }
    }
}
