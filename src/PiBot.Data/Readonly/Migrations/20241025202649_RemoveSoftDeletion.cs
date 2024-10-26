using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiBot.Data.Readonly.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSoftDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "megaprikols");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "megaprikols",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
