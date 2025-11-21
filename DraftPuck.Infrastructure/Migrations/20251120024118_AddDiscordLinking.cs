using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscordLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscordUserId",
                table: "Users",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscordUserLinkedDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "UniqueIdentifier", "FriendlyName", "Description" },
                values: new object[,]
                {
                    { new Guid("2a961b45-147e-416a-bd52-f8dd3e5e26f9"), "certified_chirper", "Certified Chirper", "Link your Discord account and join the official DraftPuck server." }
                });

            migrationBuilder.InsertData(
                table: "Banners",
                columns: new[] { "Id", "UniqueIdentifier", "AchievementId", "ImagePath" },
                values: new object[,]
                {
                    { new Guid("996b01bf-548b-4f76-aea6-0373c43f8b59"), "certified_chirper", new Guid("2a961b45-147e-416a-bd52-f8dd3e5e26f9"), "/img/banners/certified-chirper.png" }
                });

            migrationBuilder.InsertData(
                table: "Titles",
                columns: new[] { "Id", "UniqueIdentifier", "Text", "AchievementId" },
                values: new object[,]
                {
                    { new Guid("54896f36-c4c7-4556-aedc-b7c543798c08"), "certified_chirper", "Certified Chirper", new Guid("2a961b45-147e-416a-bd52-f8dd3e5e26f9") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DiscordUserLinkedDate",
                table: "Users");

            migrationBuilder.DeleteData(
            table: "Titles",
            keyColumn: "Id",
            keyValues: new object[]
            {
                new Guid("54896f36-c4c7-4556-aedc-b7c543798c08")
            });

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("996b01bf-548b-4f76-aea6-0373c43f8b59")
                });

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("2a961b45-147e-416a-bd52-f8dd3e5e26f9")
                });
        }
    }
}
