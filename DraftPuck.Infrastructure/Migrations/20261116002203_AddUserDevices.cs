using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    LastSeenUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDevices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Each user's single token becomes their first device. A token is one device, so if two users somehow share
            // one, the most recently created user keeps it.
            migrationBuilder.Sql("""
                INSERT INTO UserDevices (UserId, Token, LastSeenUtc)
                SELECT UserId, Token, SYSUTCDATETIME()
                FROM (
                    SELECT Id AS UserId,
                           LTRIM(RTRIM(FcmRegistrationToken)) AS Token,
                           ROW_NUMBER() OVER (PARTITION BY LTRIM(RTRIM(FcmRegistrationToken)) ORDER BY Created DESC) AS Rank
                    FROM Users
                    WHERE FcmRegistrationToken IS NOT NULL
                      AND LEN(LTRIM(RTRIM(FcmRegistrationToken))) BETWEEN 1 AND 450
                ) AS Tokens
                WHERE Rank = 1
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_Token",
                table: "UserDevices",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId",
                table: "UserDevices",
                column: "UserId");

            migrationBuilder.DropColumn(
                name: "FcmRegistrationToken",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FcmRegistrationToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            // Back to one token per user: their most recently seen device.
            migrationBuilder.Sql("""
                UPDATE Users SET FcmRegistrationToken = Latest.Token
                FROM Users
                JOIN (
                    SELECT UserId, Token, ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY LastSeenUtc DESC) AS Rank
                    FROM UserDevices
                ) AS Latest ON Latest.UserId = Users.Id AND Latest.Rank = 1
                """);

            migrationBuilder.DropTable(
                name: "UserDevices");
        }
    }
}
