using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_LobbyMemberId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_LobbyMemberPicks_LobbyMemberId",
                table: "LobbyMemberPicks");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_LobbyMemberId_IsDeleted",
                table: "Messages",
                columns: new[] { "LobbyMemberId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_LobbyMemberPicks_LobbyMemberId_IsActive",
                table: "LobbyMemberPicks",
                columns: new[] { "LobbyMemberId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_IsActive",
                table: "Lobbies",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_LobbyMemberId_IsDeleted",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_LobbyMemberPicks_LobbyMemberId_IsActive",
                table: "LobbyMemberPicks");

            migrationBuilder.DropIndex(
                name: "IX_Lobbies_IsActive",
                table: "Lobbies");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_LobbyMemberId",
                table: "Messages",
                column: "LobbyMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_LobbyMemberPicks_LobbyMemberId",
                table: "LobbyMemberPicks",
                column: "LobbyMemberId");
        }
    }
}
