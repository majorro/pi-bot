using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiBot.Data.Discord.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "discord_guilds",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                table: "discord_guilds");
        }
    }
}