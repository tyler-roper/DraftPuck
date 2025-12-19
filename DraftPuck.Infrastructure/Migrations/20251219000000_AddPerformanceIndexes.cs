using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Users table indexes
            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Nickname",
                table: "Users",
                column: "Nickname");

            // Lobbies table indexes
            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_JoinCode",
                table: "Lobbies",
                column: "JoinCode",
                unique: true,
                filter: "[JoinCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_Created",
                table: "Lobbies",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_CreatedBy",
                table: "Lobbies",
                column: "CreatedBy");

            // LobbyMembers table indexes
            migrationBuilder.CreateIndex(
                name: "IX_LobbyMembers_UserId",
                table: "LobbyMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LobbyMembers_LobbyId_UserId",
                table: "LobbyMembers",
                columns: new[] { "LobbyId", "UserId" });

            // Drinks table indexes
            migrationBuilder.CreateIndex(
                name: "IX_Drinks_EventId",
                table: "Drinks",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Drinks_RecipientLobbyMemberId",
                table: "Drinks",
                column: "RecipientLobbyMemberId");

            // UserRefreshTokens table indexes
            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_Token",
                table: "UserRefreshTokens",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId",
                table: "UserRefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Nickname",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Lobbies_JoinCode",
                table: "Lobbies");

            migrationBuilder.DropIndex(
                name: "IX_Lobbies_Created",
                table: "Lobbies");

            migrationBuilder.DropIndex(
                name: "IX_Lobbies_CreatedBy",
                table: "Lobbies");

            migrationBuilder.DropIndex(
                name: "IX_LobbyMembers_UserId",
                table: "LobbyMembers");

            migrationBuilder.DropIndex(
                name: "IX_LobbyMembers_LobbyId_UserId",
                table: "LobbyMembers");

            migrationBuilder.DropIndex(
                name: "IX_Drinks_EventId",
                table: "Drinks");

            migrationBuilder.DropIndex(
                name: "IX_Drinks_RecipientLobbyMemberId",
                table: "Drinks");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_Token",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_UserId",
                table: "UserRefreshTokens");
        }
    }
}
