using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievementBanners : Migration
    {
        // Banners for the twenty achievements added in AddMoreAchievements. Each banner shares its achievement's
        // identifier, and its image is /img/banners/<identifier with dashes>.png.
        private static readonly string[] Identifiers =
        [
            "sudden_death", "buzzer_beater", "first_blood", "game_winner", "man_advantage", "empty_netter", "late_call_up",
            "homer", "clean_sheet", "healthy_scratch", "goose_egg", "designated_driver", "target_practice", "spread_the_wealth",
            "bottomless_cup", "back_to_back", "season_ticket_holder", "chirp_machine", "bot_army", "playoff_beard"
        ];

        private static string IdentifierList => string.Join(", ", Identifiers.Select(id => $"'{id}'"));

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                INSERT INTO Banners (Id, UniqueIdentifier, AchievementId, ImagePath)
                SELECT NEWID(), a.UniqueIdentifier, a.Id, '/img/banners/' + REPLACE(a.UniqueIdentifier, '_', '-') + '.png'
                FROM Achievements a
                WHERE a.UniqueIdentifier IN ({IdentifierList})
                  AND NOT EXISTS (SELECT 1 FROM Banners b WHERE b.AchievementId = a.Id);

                -- Anyone who earned one of these before it had a banner gets the banner too.
                INSERT INTO UserBanners (UserId, BannerId, IsEquipped)
                SELECT ua.UserId, b.Id, 0
                FROM UserAchievements ua
                INNER JOIN Banners b ON b.AchievementId = ua.AchievementId
                WHERE b.UniqueIdentifier IN ({IdentifierList})
                  AND NOT EXISTS (SELECT 1 FROM UserBanners ub WHERE ub.UserId = ua.UserId AND ub.BannerId = b.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE ub FROM UserBanners ub INNER JOIN Banners b ON b.Id = ub.BannerId WHERE b.UniqueIdentifier IN ({IdentifierList});
                DELETE FROM Banners WHERE UniqueIdentifier IN ({IdentifierList});
                """);
        }
    }
}
