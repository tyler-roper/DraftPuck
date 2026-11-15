using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWavedOffAchievement : Migration
    {
        // Waved Off: the recipient's side of a drink invalidated by a scoring change (False Alarm is the giver's side).
        // It has a title but no banner yet.
        private static readonly Guid AchievementId = new("c9758f74-ad4d-4a86-bf7f-065fba247794");
        private static readonly Guid TitleId = new("fa897743-0f75-4209-86c1-0f18f055f431");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "UniqueIdentifier", "FriendlyName", "Description" },
                values: new object[,]
                {
                    { AchievementId, "waved_off", "Waved Off", "Drink a beer for a goal that was later waved off by an official review." }
                });

            migrationBuilder.InsertData(
                table: "Titles",
                columns: new[] { "Id", "UniqueIdentifier", "Text", "AchievementId" },
                values: new object[,]
                {
                    { TitleId, "waved_off", "Waved Off", AchievementId }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM UserTitles WHERE TitleId = '{TitleId}'; DELETE FROM UserAchievements WHERE AchievementId = '{AchievementId}';");
            migrationBuilder.DeleteData(table: "Titles", keyColumn: "Id", keyValue: TitleId);
            migrationBuilder.DeleteData(table: "Achievements", keyColumn: "Id", keyValue: AchievementId);
        }
    }
}
