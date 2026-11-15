using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWavedOffBanner : Migration
    {
        private static readonly Guid BannerId = new("eb2fc3e1-ed94-4b9e-9f7a-6c78902a2516");
        private static readonly Guid AchievementId = new("c9758f74-ad4d-4a86-bf7f-065fba247794"); // waved_off

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Banners",
                columns: new[] { "Id", "UniqueIdentifier", "AchievementId", "ImagePath" },
                values: new object[,]
                {
                    { BannerId, "waved_off", AchievementId, "/img/banners/waved-off.png" }
                });

            // Anyone who earned Waved Off before it had a banner gets the banner too.
            migrationBuilder.Sql($"""
                INSERT INTO UserBanners (UserId, BannerId, IsEquipped)
                SELECT ua.UserId, '{BannerId}', 0
                FROM UserAchievements ua
                WHERE ua.AchievementId = '{AchievementId}'
                  AND NOT EXISTS (SELECT 1 FROM UserBanners ub WHERE ub.UserId = ua.UserId AND ub.BannerId = '{BannerId}');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM UserBanners WHERE BannerId = '{BannerId}';");
            migrationBuilder.DeleteData(table: "Banners", keyColumn: "Id", keyValue: BannerId);
        }
    }
}
