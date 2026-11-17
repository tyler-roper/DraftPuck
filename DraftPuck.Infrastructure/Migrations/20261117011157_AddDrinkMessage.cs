using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDrinkMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "Drinks",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Message",
                table: "Drinks");
        }
    }
}
