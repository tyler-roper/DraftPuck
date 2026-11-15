using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAchievementDuplicates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Parallel achievement checks could award the same achievement (and its banner and title) twice, and bots
            // earned achievements. Clean both up before the unique indexes go on: keep each user's earliest award, and
            // the equipped copy of a banner or title.
            migrationBuilder.Sql("""
                DELETE ua FROM UserAchievements ua INNER JOIN Users u ON u.Id = ua.UserId WHERE u.IsBot = 1;
                DELETE ub FROM UserBanners ub INNER JOIN Users u ON u.Id = ub.UserId WHERE u.IsBot = 1;
                DELETE ut FROM UserTitles ut INNER JOIN Users u ON u.Id = ut.UserId WHERE u.IsBot = 1;

                WITH Ranked AS (SELECT ROW_NUMBER() OVER (PARTITION BY UserId, AchievementId ORDER BY DateEarned, Id) AS Copy FROM UserAchievements)
                DELETE FROM Ranked WHERE Copy > 1;

                WITH Ranked AS (SELECT ROW_NUMBER() OVER (PARTITION BY UserId, BannerId ORDER BY IsEquipped DESC, Id) AS Copy FROM UserBanners)
                DELETE FROM Ranked WHERE Copy > 1;

                WITH Ranked AS (SELECT ROW_NUMBER() OVER (PARTITION BY UserId, TitleId ORDER BY IsEquipped DESC, Id) AS Copy FROM UserTitles)
                DELETE FROM Ranked WHERE Copy > 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_UserTitles_UserId",
                table: "UserTitles");

            migrationBuilder.DropIndex(
                name: "IX_UserBanners_UserId",
                table: "UserBanners");

            migrationBuilder.DropIndex(
                name: "IX_UserAchievements_UserId",
                table: "UserAchievements");

            migrationBuilder.CreateIndex(
                name: "IX_UserTitles_UserId_TitleId",
                table: "UserTitles",
                columns: new[] { "UserId", "TitleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBanners_UserId_BannerId",
                table: "UserBanners",
                columns: new[] { "UserId", "BannerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_AchievementId",
                table: "UserAchievements",
                columns: new[] { "UserId", "AchievementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserTitles_UserId_TitleId",
                table: "UserTitles");

            migrationBuilder.DropIndex(
                name: "IX_UserBanners_UserId_BannerId",
                table: "UserBanners");

            migrationBuilder.DropIndex(
                name: "IX_UserAchievements_UserId_AchievementId",
                table: "UserAchievements");

            migrationBuilder.CreateIndex(
                name: "IX_UserTitles_UserId",
                table: "UserTitles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBanners_UserId",
                table: "UserBanners",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId",
                table: "UserAchievements",
                column: "UserId");
        }
    }
}
