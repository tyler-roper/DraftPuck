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
            migrationBuilder.DropIndex(
                name: "IX_Drinks_LobbyMemberPickId",
                table: "Drinks");

            migrationBuilder.AlterColumn<string>(
                name: "Nickname",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "UserRefreshTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

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

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_Token",
                table: "UserRefreshTokens",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_LobbyMembers_LobbyId_UserId",
                table: "LobbyMembers",
                columns: new[] { "LobbyId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_LobbyMemberPicks_Game_Player_Active",
                table: "LobbyMemberPicks",
                columns: new[] { "GameId", "PlayerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_LobbyMemberPicks_LobbyMemberId",
                table: "LobbyMemberPicks",
                column: "LobbyMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_Created",
                table: "Lobbies",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_JoinCode",
                table: "Lobbies",
                column: "JoinCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drinks_EventId",
                table: "Drinks",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Drinks_Pick_Event",
                table: "Drinks",
                columns: new[] { "LobbyMemberPickId", "EventId" });
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
                name: "IX_UserRefreshTokens_Token",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_LobbyMembers_LobbyId_UserId",
                table: "LobbyMembers");

            migrationBuilder.DropIndex(
                name: "IX_LobbyMemberPicks_Game_Player_Active",
                table: "LobbyMemberPicks");

            migrationBuilder.DropIndex(
                name: "IX_LobbyMemberPicks_LobbyMemberId",
                table: "LobbyMemberPicks");

            migrationBuilder.DropIndex(
                name: "IX_Lobbies_Created",
                table: "Lobbies");

            migrationBuilder.DropIndex(
                name: "IX_Lobbies_JoinCode",
                table: "Lobbies");

            migrationBuilder.DropIndex(
                name: "IX_Drinks_EventId",
                table: "Drinks");

            migrationBuilder.DropIndex(
                name: "IX_Drinks_Pick_Event",
                table: "Drinks");

            migrationBuilder.AlterColumn<string>(
                name: "Nickname",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "UserRefreshTokens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Drinks_LobbyMemberPickId",
                table: "Drinks",
                column: "LobbyMemberPickId");
        }
    }
}
