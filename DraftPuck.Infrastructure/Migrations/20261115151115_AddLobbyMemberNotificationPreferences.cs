using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLobbyMemberNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChatNotificationPreference",
                table: "LobbyMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DrinkAwardedNotificationPreference",
                table: "LobbyMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DrinkReceivedNotificationPreference",
                table: "LobbyMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PickingStartedNotificationPreference",
                table: "LobbyMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Settings used to be per user. Copy each user's settings onto their existing lobby memberships, so
            // nobody's notifications change; the user's settings become their defaults for new lobbies.
            migrationBuilder.Sql("""
                UPDATE lm
                SET lm.DrinkReceivedNotificationPreference = u.DrinkReceivedNotificationPreference,
                    lm.DrinkAwardedNotificationPreference = u.DrinkAwardedNotificationPreference,
                    lm.ChatNotificationPreference = u.ChatNotificationPreference,
                    lm.PickingStartedNotificationPreference = u.PickingStartedNotificationPreference
                FROM LobbyMembers lm
                INNER JOIN Users u ON u.Id = lm.UserId
                WHERE lm.IsBot = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChatNotificationPreference",
                table: "LobbyMembers");

            migrationBuilder.DropColumn(
                name: "DrinkAwardedNotificationPreference",
                table: "LobbyMembers");

            migrationBuilder.DropColumn(
                name: "DrinkReceivedNotificationPreference",
                table: "LobbyMembers");

            migrationBuilder.DropColumn(
                name: "PickingStartedNotificationPreference",
                table: "LobbyMembers");
        }
    }
}
