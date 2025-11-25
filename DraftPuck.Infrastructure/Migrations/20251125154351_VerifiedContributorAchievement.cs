using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VerifiedContributorAchievement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "UniqueIdentifier", "FriendlyName", "Description" },
                values: new object[,]
                {
                    { new Guid("c248f411-972f-4401-8cba-c416a5123b06"), "verified_contributor", "Verified Contributor", "Merge a pull request into the official DraftPuck git repo." }
                });

            migrationBuilder.InsertData(
                table: "Banners",
                columns: new[] { "Id", "UniqueIdentifier", "AchievementId", "ImagePath" },
                values: new object[,]
                {
                    { new Guid("b88f32b2-8cf8-4ea4-9519-a4be486f69b0"), "verified_contributor", new Guid("c248f411-972f-4401-8cba-c416a5123b06"), "/img/banners/verified-contributor.png" }
                });

            migrationBuilder.InsertData(
                table: "Titles",
                columns: new[] { "Id", "UniqueIdentifier", "Text", "AchievementId" },
                values: new object[,]
                {
                    { new Guid("6a825c7a-78bb-4580-b215-3f6df070f0d9"), "verified_contributor", "Verified Contributor", new Guid("c248f411-972f-4401-8cba-c416a5123b06") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Titles",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("6a825c7a-78bb-4580-b215-3f6df070f0d9")
                });

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("b88f32b2-8cf8-4ea4-9519-a4be486f69b0")
                });

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("c248f411-972f-4401-8cba-c416a5123b06")
                });
        }
    }
}
