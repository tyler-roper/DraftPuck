using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    UniqueIdentifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FriendlyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Info = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LobbyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    TimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Subtext = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PlayerId = table.Column<int>(type: "int", nullable: true),
                    Player2Id = table.Column<int>(type: "int", nullable: true),
                    TeamId = table.Column<int>(type: "int", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    GameEventId = table.Column<int>(type: "int", nullable: true),
                    GameId = table.Column<int>(type: "int", nullable: true),
                    IsSent = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((0))"),
                    LastSendAttempt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SendAttempts = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((0))"),
                    LobbyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LobbyEventType = table.Column<int>(type: "int", nullable: false),
                    LobbyMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LobbyMember2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LobbyEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Nickname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsGuest = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((1))"),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((0))"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((1))"),
                    IsBot = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((0))"),
                    AvatarPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    FcmRegistrationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DrinkReceivedNotificationPreference = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((0))"),
                    DrinkAwardedNotificationPreference = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((0))"),
                    ChatNotificationPreference = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((0))"),
                    PickingStartedNotificationPreference = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((0))"),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Banners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    UniqueIdentifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AchievementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Banners_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Titles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    UniqueIdentifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AchievementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Titles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Titles_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lobbies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((1))"),
                    JoinCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", maxLength: 50, nullable: false),
                    PicksPerTeam = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((1))"),
                    IsBotAutoPickingEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((0))"),
                    GameIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lobbies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lobbies_People",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateEarned = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expires = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    CreatedByIp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Revoked = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonRevoked = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AntiCsrfToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBanners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BannerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEquipped = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((0))")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBanners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBanners_Banners_BannerId",
                        column: x => x.BannerId,
                        principalTable: "Banners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBanners_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEquipped = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((0))")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTitles_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTitles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LobbyMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    LobbyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Joined = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsBot = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((0))"),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((0))"),
                    BotPickStyle = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LobbyMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LobbyMembers_Lobbies",
                        column: x => x.LobbyId,
                        principalTable: "Lobbies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LobbyMembers_People",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LobbyMemberPicks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    LobbyMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((1))")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LobbyMemberPicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LobbyMemberPicks_LobbyMembers",
                        column: x => x.LobbyMemberId,
                        principalTable: "LobbyMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    LobbyMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sent = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((0))")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_LobbyMembers",
                        column: x => x.LobbyMemberId,
                        principalTable: "LobbyMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Drinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    LobbyMemberPickId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientLobbyMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    Assigned = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drinks_LobbyMemberPicks",
                        column: x => x.LobbyMemberPickId,
                        principalTable: "LobbyMemberPicks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Drinks_LobbyMembers",
                        column: x => x.RecipientLobbyMemberId,
                        principalTable: "LobbyMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_UniqueIdentifier",
                table: "Achievements",
                column: "UniqueIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Banners_AchievementId",
                table: "Banners",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_Drinks_LobbyMemberPickId",
                table: "Drinks",
                column: "LobbyMemberPickId");

            migrationBuilder.CreateIndex(
                name: "IX_Drinks_RecipientLobbyMemberId",
                table: "Drinks",
                column: "RecipientLobbyMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Lobbies_CreatedBy",
                table: "Lobbies",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LobbyMemberPicks_LobbyMemberId",
                table: "LobbyMemberPicks",
                column: "LobbyMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_LobbyMembers_LobbyId",
                table: "LobbyMembers",
                column: "LobbyId");

            migrationBuilder.CreateIndex(
                name: "IX_LobbyMembers_UserId",
                table: "LobbyMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_LobbyMemberId",
                table: "Messages",
                column: "LobbyMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Titles_AchievementId",
                table: "Titles",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId",
                table: "UserAchievements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBanners_BannerId",
                table: "UserBanners",
                column: "BannerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBanners_UserId",
                table: "UserBanners",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId",
                table: "UserRefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTitles_TitleId",
                table: "UserTitles",
                column: "TitleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTitles_UserId",
                table: "UserTitles",
                column: "UserId");

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "UniqueIdentifier", "FriendlyName", "Description" },
                values: new object[,]
                {
                    { new Guid("0D8166A5-F952-4D2F-8FC2-053896E36F48"), "party_crasher", "Party Crasher", "Join your very first lobby." },
                    { new Guid("40B2FFD6-6034-49C1-96A9-06E665B067A5"), "rookie_host", "Rookie Host", "Create your very first lobby." },
                    { new Guid("A1AFC1C6-D680-4881-A092-079BBC2037AA"), "first_rounder", "First Rounder", "Give out your very first drink." },
                    { new Guid("74BDA6B0-ED3E-434B-AEB6-0E02DF3E16E6"), "terminated", "Terminated", "Receive 5 drinks specifically from Bots in a single lobby." },
                    { new Guid("90C36B77-F327-4FEF-9CA7-116D4DD21A1C"), "party_starter", "Party Starter", "Host a lobby with at least 8 human players." },
                    { new Guid("DD8F4368-6015-47AA-896A-1743E9532B9D"), "five_hole", "Five Hole", "Receive 5 total drinks (from any combination of human players or bots) in a single lobby." },
                    { new Guid("BE51F9FC-D7E9-4B6D-B6E7-246EB90375A6"), "the_gambler", "The Gambler", "Choose a player from the bottom third of their team in goals, and have them score." },
                    { new Guid("66EAAA61-B7B6-479B-B7B9-2B7835F1EE98"), "hot_hand", "Hot Hand", "Have two of your chosen players score in the same minute." },
                    { new Guid("EB263AD4-5AFD-4570-908C-4612D77866AE"), "light_the_lamp", "Light The Lamp", "Have a player score within 1 minute of choosing them." },
                    { new Guid("D96D0092-D4DA-4595-AC90-4812AADB6707"), "mad_hatter", "Mad Hatter", "Have one of your chosen players score a hattrick (3 goals)." },
                    { new Guid("D510FEAD-AEFF-474F-B750-6C1B7EC1EE35"), "the_bartender", "The Bartender", "Give out 5 drinks in a single lobby." },
                    { new Guid("A313DB64-3473-4D49-93F1-6C5773EF4612"), "puppet_master", "Puppet Master", "Create a lobby where the Bots collectively assign 3 or more drinks." },
                    { new Guid("D6BE167E-E069-4872-AC26-7FC276C59CFA"), "incognito_mode", "Incognito Mode", "Change your name 3 times in a single lobby." },
                    { new Guid("86E7D155-4AD7-4D8F-A470-8206F7447B2B"), "pinged", "Pinged", "Receive a drink from a Bot." },
                    { new Guid("5A95E669-1ADD-4C49-9E29-86293180B1B5"), "early_riser", "Early Riser", "Join or create a lobby 3+ hours before the first game starts." },
                    { new Guid("7267E6ED-80D1-480A-87DF-875177822260"), "8_bit", "8-Bit", "Give and receive a drink from the same player within 8 minutes." },
                    { new Guid("AD968BCF-97FD-442E-A5FD-A56753BBC93E"), "initiated", "Initiated", "Receive your very first drink." },
                    { new Guid("94D08225-1556-4A48-88C9-AF78A642F84D"), "the_cup", "The Cup", "Give out the most drinks in a lobby with at least 5 human players." },
                    { new Guid("676EB523-44DA-48F5-BCBE-B2EBD6BB7E6F"), "random_number_generator", "Random Number Generator", "Receive 3 drinks from Bots in a single lobby." },
                    { new Guid("DBEB90C2-03C5-4E77-8477-BAD52407872A"), "royalty", "Royalty", "Create a lobby on 10 different days." },
                    { new Guid("5DD310B0-1D6D-475B-9FC6-D1DE901C82EA"), "false_alarm", "False Alarm", "Have a drink assignment revoked due to an official scoring change." },
                    { new Guid("D60F0DEE-A8E9-4177-8956-D94D378969ED"), "full_auto", "Full Auto", "Give out 3 drinks in less than 3 minutes." },
                    { new Guid("1141DB2F-7190-47B7-A9B3-E1B831329D5E"), "in_plain_sight", "In Plain Sight", "Give out 3 drinks without being assigned any in return." },
                    { new Guid("259D060F-8E14-4AE9-8D86-FE4B99D7EE77"), "snake_eyes", "Snake Eyes", "Pick only 2 players and have both of them score." }
                });

            migrationBuilder.InsertData(
                table: "Banners",
                columns: new[] { "Id", "UniqueIdentifier", "AchievementId", "ImagePath" },
                values: new object[,]
                {
                    { new Guid("AF5C9D3D-C6DE-437C-B006-00A85D9F27AF"), "party_crasher", new Guid("0D8166A5-F952-4D2F-8FC2-053896E36F48"), "/img/banners/party-crasher.png" },
                    { new Guid("CA917CA5-D1E8-4A02-B8FD-0423BC7573EF"), "the_bartender", new Guid("D510FEAD-AEFF-474F-B750-6C1B7EC1EE35"), "/img/banners/the-bartender.png" },
                    { new Guid("16D59319-98EC-4C22-9E59-04DF925D97D3"), "full_auto", new Guid("D60F0DEE-A8E9-4177-8956-D94D378969ED"), "/img/banners/full-auto.png" },
                    { new Guid("BBAFA9A8-CF8C-477A-818E-09B237E69BDD"), "incognito_mode", new Guid("D6BE167E-E069-4872-AC26-7FC276C59CFA"), "/img/banners/incognito-mode.png" },
                    { new Guid("32073203-C60D-4219-94E6-0DE0785B419A"), "default", null, "/img/banners/default.png" },
                    { new Guid("F0B6D4D7-68E9-4F5A-AFD6-15C9404B9D46"), "in_plain_sight", new Guid("1141DB2F-7190-47B7-A9B3-E1B831329D5E"), "/img/banners/in-plain-sight.png" },
                    { new Guid("3080FDD3-4981-4CB9-86BE-1CF94434F601"), "first_rounder", new Guid("A1AFC1C6-D680-4881-A092-079BBC2037AA"), "/img/banners/first-rounder.png" },
                    { new Guid("9009F829-AC49-4D8F-A82B-2639525D6E3F"), "zombie", null, "/img/banners/zombie.png" },
                    { new Guid("02E38185-E087-48AC-A463-29C92C9BCF13"), "bender", null, "/img/banners/bender.png" },
                    { new Guid("63C57B3F-5EE4-4CF2-9CC2-308DACA3B254"), "pinged", new Guid("86E7D155-4AD7-4D8F-A470-8206F7447B2B"), "/img/banners/pinged.png" },
                    { new Guid("8A7D3290-1FE1-4A7C-887F-37DB1C8BF12C"), "terminated", new Guid("74BDA6B0-ED3E-434B-AEB6-0E02DF3E16E6"), "/img/banners/terminated.png" },
                    { new Guid("ACDB14A2-1DE3-4D09-87C9-3A3336011BEF"), "five_hole", new Guid("DD8F4368-6015-47AA-896A-1743E9532B9D"), "/img/banners/five-hole.png" },
                    { new Guid("FF6774E9-8C3C-4BED-A783-3D860AAA39E9"), "the_gambler", new Guid("BE51F9FC-D7E9-4B6D-B6E7-246EB90375A6"), "/img/banners/the-gambler.png" },
                    { new Guid("F5D09D8C-B6BD-41FD-96F7-487C6CDB36F8"), "puppet_master", new Guid("A313DB64-3473-4D49-93F1-6C5773EF4612"), "/img/banners/puppet-master.png" },
                    { new Guid("54C779EF-D433-43BB-9963-55C894C22D49"), "party_starter", new Guid("90C36B77-F327-4FEF-9CA7-116D4DD21A1C"), "/img/banners/party-starter.png" },
                    { new Guid("A9A05633-5252-4972-968A-6211ABF0E97C"), "rookie_host", new Guid("40B2FFD6-6034-49C1-96A9-06E665B067A5"), "/img/banners/rookie-host.png" },
                    { new Guid("59ADF00F-3503-4110-864A-6340927552A1"), "royalty", new Guid("DBEB90C2-03C5-4E77-8477-BAD52407872A"), "/img/banners/royalty.png" },
                    { new Guid("1E087F29-C301-4374-8EA0-68A145ECCA08"), "random_number_generator", new Guid("676EB523-44DA-48F5-BCBE-B2EBD6BB7E6F"), "/img/banners/random-number-generator.png" },
                    { new Guid("DE2EAC44-4243-48BC-864E-7D609C755BEB"), "early_riser", new Guid("5A95E669-1ADD-4C49-9E29-86293180B1B5"), "/img/banners/early-riser.png" },
                    { new Guid("A42E2B56-81BB-4E32-9E47-8014A8DF51D4"), "false_alarm", new Guid("5DD310B0-1D6D-475B-9FC6-D1DE901C82EA"), "/img/banners/false-alarm.png" },
                    { new Guid("54AB5FA0-97FF-477C-A216-84F0C4C65623"), "shattered_glass", null, "/img/banners/shattered-glass.png" },
                    { new Guid("A55853E2-13A8-4FCF-B07A-9B46D1F86294"), "goalie_beer", null, "/img/banners/goalie-beer.png" },
                    { new Guid("490912D6-A290-498B-94A2-B10E7619A78F"), "mad_hatter", new Guid("D96D0092-D4DA-4595-AC90-4812AADB6707"), "/img/banners/mad-hatter.png" },
                    { new Guid("DBEBDC18-824B-4F83-82CA-BABB28FEA79F"), "snake_eyes", new Guid("259D060F-8E14-4AE9-8D86-FE4B99D7EE77"), "/img/banners/snake-eyes.png" },
                    { new Guid("021A9DBC-B450-4C75-964F-BF30F6855260"), "the_cup", new Guid("94D08225-1556-4A48-88C9-AF78A642F84D"), "/img/banners/the-cup.png" },
                    { new Guid("D90A2897-3DC6-49C9-AF29-C23B0091CAF9"), "light_the_lamp", new Guid("EB263AD4-5AFD-4570-908C-4612D77866AE"), "/img/banners/light-the-lamp.png" },
                    { new Guid("B91747F1-F1FB-4F89-AE18-CB500E9AF974"), "hot_hand", new Guid("66EAAA61-B7B6-479B-B7B9-2B7835F1EE98"), "/img/banners/hot-hand.png" },
                    { new Guid("C56966BA-3934-493F-969A-D9C6671C09C8"), "initiated", new Guid("AD968BCF-97FD-442E-A5FD-A56753BBC93E"), "/img/banners/initiated.png" },
                    { new Guid("2816C75E-577F-4D69-AF3E-EB2AEE1DDB08"), "8_bit", new Guid("7267E6ED-80D1-480A-87DF-875177822260"), "/img/banners/8-bit.png" },
                    { new Guid("F0AB43B0-41E4-4475-8FA2-EBD2318AA7A4"), "tusk", null, "/img/banners/tusk.png" }
                });

            migrationBuilder.InsertData(
                table: "Titles",
                columns: new[] { "Id", "UniqueIdentifier", "Text", "AchievementId" },
                values: new object[,]
                {
                    { new Guid("43865FAC-9B35-4708-A659-0C58D09242ED"), "party_starter", "Party Starter", new Guid("90C36B77-F327-4FEF-9CA7-116D4DD21A1C") },
                    { new Guid("1A81EB6A-23C6-4C34-B1AD-0E0B4C9F34A9"), "hot_hand", "Hot Hand", new Guid("66EAAA61-B7B6-479B-B7B9-2B7835F1EE98") },
                    { new Guid("61DC1A23-18EE-4FC1-B1EA-0EE92E8CD02A"), "pigeon", "Pigeon", null },
                    { new Guid("F0D7E543-279A-4D88-9E2C-0FB8A3100883"), "royalty", "Royalty", new Guid("DBEB90C2-03C5-4E77-8477-BAD52407872A") },
                    { new Guid("5864C8C7-217B-4799-92CD-100BF0AF98AF"), "terminated", "Terminated", new Guid("74BDA6B0-ED3E-434B-AEB6-0E02DF3E16E6") },
                    { new Guid("E073CCFA-E64B-435D-9FAD-119E0A37B67B"), "early_riser", "Early Riser", new Guid("5A95E669-1ADD-4C49-9E29-86293180B1B5") },
                    { new Guid("1BB06EBA-5905-46DB-B601-2001E1FD308A"), "beer_leaguer", "Beer Leaguer", null },
                    { new Guid("7E51DB40-ED8E-4B18-B0DD-24064475C16A"), "bender", "Bender", null },
                    { new Guid("78AA98D0-B089-44DB-87C9-2D136E14A8BD"), "the_bartender", "The Bartender", new Guid("D510FEAD-AEFF-474F-B750-6C1B7EC1EE35") },
                    { new Guid("07357A00-E6C8-423D-AF24-3AA67EEF0F09"), "random_number_generator", "Random Number Generator", new Guid("676EB523-44DA-48F5-BCBE-B2EBD6BB7E6F") },
                    { new Guid("984548D8-7729-4D47-BFE5-3F4BF165A06A"), "the_gambler", "The Gambler", new Guid("BE51F9FC-D7E9-4B6D-B6E7-246EB90375A6") },
                    { new Guid("A54040D6-8766-44D7-A5D9-470E2436BCA0"), "pylon", "Pylon", null },
                    { new Guid("6853C2E6-7A94-4D6B-B3E1-4DDF392D6E98"), "false_alarm", "False Alarm", new Guid("5DD310B0-1D6D-475B-9FC6-D1DE901C82EA") },
                    { new Guid("91B8DA94-650E-4B89-BF3B-4EF0D046E670"), "8_bit", "8-Bit", new Guid("7267E6ED-80D1-480A-87DF-875177822260") },
                    { new Guid("C55E85AF-FCE7-491A-BDE4-60563EB7ED1D"), "extra_skater", "Extra Skater", null },
                    { new Guid("7BBD6C21-C139-4C63-9D6A-60F2691E6238"), "five_hole", "Five Hole", new Guid("DD8F4368-6015-47AA-896A-1743E9532B9D") },
                    { new Guid("25238422-6EED-47A2-BBB0-70065F54544D"), "full_auto", "Full Auto", new Guid("D60F0DEE-A8E9-4177-8956-D94D378969ED") },
                    { new Guid("7B67E835-8FFC-493C-A2EA-73BE28A862D5"), "luck_averse", "Luck Averse", null },
                    { new Guid("E9195FF5-F3EE-472F-AA59-7C5777D95F7A"), "zamboni_driver", "Zamboni Driver", null },
                    { new Guid("BAAD7852-9A61-4453-BA69-85ADA6902CF1"), "pinged", "Pinged", new Guid("86E7D155-4AD7-4D8F-A470-8206F7447B2B") },
                    { new Guid("0D99B8B7-6D80-4AC5-8FEC-8EFAAB659E10"), "default", "New Here", null },
                    { new Guid("08A7C83E-EE36-4FE3-AA73-9A351DEBB5D8"), "puppet_master", "Puppet Master", new Guid("A313DB64-3473-4D49-93F1-6C5773EF4612") },
                    { new Guid("D09911F8-2655-4088-A5CC-BA503C372280"), "in_plain_sight", "In Plain Sight", new Guid("1141DB2F-7190-47B7-A9B3-E1B831329D5E") },
                    { new Guid("097EF48E-899D-4BF0-97C5-D2FD95B32E9F"), "dirty_dangles", "Dirty Dangles", null },
                    { new Guid("C2F44CA5-047C-4C76-A1AD-D564178551A6"), "light_the_lamp", "Light The Lamp", new Guid("EB263AD4-5AFD-4570-908C-4612D77866AE") },
                    { new Guid("FCBC8810-B6B7-4D7F-B955-D653977111E9"), "initiated", "Initiated", new Guid("AD968BCF-97FD-442E-A5FD-A56753BBC93E") },
                    { new Guid("F5047905-686F-45D9-9EC8-DC0707CDF862"), "party_crasher", "Party Crasher", new Guid("0D8166A5-F952-4D2F-8FC2-053896E36F48") },
                    { new Guid("75D8CF21-AEF9-49F4-80F1-DC358D1B5AAD"), "snake_eyes", "Snake Eyes", new Guid("259D060F-8E14-4AE9-8D86-FE4B99D7EE77") },
                    { new Guid("95419FF6-CDB8-4557-9EE0-DD38F44505ED"), "mad_hatter", "Mad Hatter", new Guid("D96D0092-D4DA-4595-AC90-4812AADB6707") },
                    { new Guid("DB2E060D-6D20-46D5-80E7-E2C46B59B482"), "ebug", "EBUG", null },
                    { new Guid("9CF6BD9F-04B8-45C4-8077-EDD0782D91E9"), "rookie_host", "Rookie Host", new Guid("40B2FFD6-6034-49C1-96A9-06E665B067A5") },
                    { new Guid("01110D4E-574E-4C6C-8E8C-EF1BB7A6C0F7"), "the_cup", "The Cup", new Guid("94D08225-1556-4A48-88C9-AF78A642F84D") },
                    { new Guid("3ACC1EAF-A9A9-411C-911A-EFA67D2EF5C5"), "injured_reserve", "Injured Reserve", null },
                    { new Guid("36EAA22A-A572-40FA-8460-F184EB530A96"), "first_rounder", "First Rounder", new Guid("A1AFC1C6-D680-4881-A092-079BBC2037AA") },
                    { new Guid("DE8E4C1F-C7C2-4AA2-8B30-FFAF9D18417F"), "incognito_mode", "Incognito Mode", new Guid("D6BE167E-E069-4872-AC26-7FC276C59CFA") }
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
                new Guid("43865FAC-9B35-4708-A659-0C58D09242ED"),
                new Guid("1A81EB6A-23C6-4C34-B1AD-0E0B4C9F34A9"),
                new Guid("61DC1A23-18EE-4FC1-B1EA-0EE92E8CD02A"),
                new Guid("F0D7E543-279A-4D88-9E2C-0FB8A3100883"),
                new Guid("5864C8C7-217B-4799-92CD-100BF0AF98AF"),
                new Guid("E073CCFA-E64B-435D-9FAD-119E0A37B67B"),
                new Guid("1BB06EBA-5905-46DB-B601-2001E1FD308A"),
                new Guid("7E51DB40-ED8E-4B18-B0DD-24064475C16A"),
                new Guid("78AA98D0-B089-44DB-87C9-2D136E14A8BD"),
                new Guid("07357A00-E6C8-423D-AF24-3AA67EEF0F09"),
                new Guid("984548D8-7729-4D47-BFE5-3F4BF165A06A"),
                new Guid("A54040D6-8766-44D7-A5D9-470E2436BCA0"),
                new Guid("6853C2E6-7A94-4D6B-B3E1-4DDF392D6E98"),
                new Guid("91B8DA94-650E-4B89-BF3B-4EF0D046E670"),
                new Guid("C55E85AF-FCE7-491A-BDE4-60563EB7ED1D"),
                new Guid("7BBD6C21-C139-4C63-9D6A-60F2691E6238"),
                new Guid("25238422-6EED-47A2-BBB0-70065F54544D"),
                new Guid("7B67E835-8FFC-493C-A2EA-73BE28A862D5"),
                new Guid("E9195FF5-F3EE-472F-AA59-7C5777D95F7A"),
                new Guid("BAAD7852-9A61-4453-BA69-85ADA6902CF1"),
                new Guid("0D99B8B7-6D80-4AC5-8FEC-8EFAAB659E10"),
                new Guid("08A7C83E-EE36-4FE3-AA73-9A351DEBB5D8"),
                new Guid("D09911F8-2655-4088-A5CC-BA503C372280"),
                new Guid("097EF48E-899D-4BF0-97C5-D2FD95B32E9F"),
                new Guid("C2F44CA5-047C-4C76-A1AD-D564178551A6"),
                new Guid("FCBC8810-B6B7-4D7F-B955-D653977111E9"),
                new Guid("F5047905-686F-45D9-9EC8-DC0707CDF862"),
                new Guid("75D8CF21-AEF9-49F4-80F1-DC358D1B5AAD"),
                new Guid("95419FF6-CDB8-4557-9EE0-DD38F44505ED"),
                new Guid("DB2E060D-6D20-46D5-80E7-E2C46B59B482"),
                new Guid("9CF6BD9F-04B8-45C4-8077-EDD0782D91E9"),
                new Guid("01110D4E-574E-4C6C-8E8C-EF1BB7A6C0F7"),
                new Guid("3ACC1EAF-A9A9-411C-911A-EFA67D2EF5C5"),
                new Guid("36EAA22A-A572-40FA-8460-F184EB530A96"),
                new Guid("DE8E4C1F-C7C2-4AA2-8B30-FFAF9D18417F")
            });

            migrationBuilder.DeleteData(
                table: "Banners",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("AF5C9D3D-C6DE-437C-B006-00A85D9F27AF"),
                    new Guid("CA917CA5-D1E8-4A02-B8FD-0423BC7573EF"),
                    new Guid("16D59319-98EC-4C22-9E59-04DF925D97D3"),
                    new Guid("BBAFA9A8-CF8C-477A-818E-09B237E69BDD"),
                    new Guid("32073203-C60D-4219-94E6-0DE0785B419A"),
                    new Guid("F0B6D4D7-68E9-4F5A-AFD6-15C9404B9D46"),
                    new Guid("3080FDD3-4981-4CB9-86BE-1CF94434F601"),
                    new Guid("9009F829-AC49-4D8F-A82B-2639525D6E3F"),
                    new Guid("02E38185-E087-48AC-A463-29C92C9BCF13"),
                    new Guid("63C57B3F-5EE4-4CF2-9CC2-308DACA3B254"),
                    new Guid("8A7D3290-1FE1-4A7C-887F-37DB1C8BF12C"),
                    new Guid("ACDB14A2-1DE3-4D09-87C9-3A3336011BEF"),
                    new Guid("FF6774E9-8C3C-4BED-A783-3D860AAA39E9"),
                    new Guid("F5D09D8C-B6BD-41FD-96F7-487C6CDB36F8"),
                    new Guid("54C779EF-D433-43BB-9963-55C894C22D49"),
                    new Guid("A9A05633-5252-4972-968A-6211ABF0E97C"),
                    new Guid("59ADF00F-3503-4110-864A-6340927552A1"),
                    new Guid("1E087F29-C301-4374-8EA0-68A145ECCA08"),
                    new Guid("DE2EAC44-4243-48BC-864E-7D609C755BEB"),
                    new Guid("A42E2B56-81BB-4E32-9E47-8014A8DF51D4"),
                    new Guid("54AB5FA0-97FF-477C-A216-84F0C4C65623"),
                    new Guid("A55853E2-13A8-4FCF-B07A-9B46D1F86294"),
                    new Guid("490912D6-A290-498B-94A2-B10E7619A78F"),
                    new Guid("DBEBDC18-824B-4F83-82CA-BABB28FEA79F"),
                    new Guid("021A9DBC-B450-4C75-964F-BF30F6855260"),
                    new Guid("D90A2897-3DC6-49C9-AF29-C23B0091CAF9"),
                    new Guid("B91747F1-F1FB-4F89-AE18-CB500E9AF974"),
                    new Guid("C56966BA-3934-493F-969A-D9C6671C09C8"),
                    new Guid("2816C75E-577F-4D69-AF3E-EB2AEE1DDB08"),
                    new Guid("F0AB43B0-41E4-4475-8FA2-EBD2318AA7A4")
                });

            migrationBuilder.DeleteData(
                table: "Achievements",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("0D8166A5-F952-4D2F-8FC2-053896E36F48"),
                    new Guid("40B2FFD6-6034-49C1-96A9-06E665B067A5"),
                    new Guid("A1AFC1C6-D680-4881-A092-079BBC2037AA"),
                    new Guid("74BDA6B0-ED3E-434B-AEB6-0E02DF3E16E6"),
                    new Guid("90C36B77-F327-4FEF-9CA7-116D4DD21A1C"),
                    new Guid("DD8F4368-6015-47AA-896A-1743E9532B9D"),
                    new Guid("BE51F9FC-D7E9-4B6D-B6E7-246EB90375A6"),
                    new Guid("66EAAA61-B7B6-479B-B7B9-2B7835F1EE98"),
                    new Guid("EB263AD4-5AFD-4570-908C-4612D77866AE"),
                    new Guid("D96D0092-D4DA-4595-AC90-4812AADB6707"),
                    new Guid("D510FEAD-AEFF-474F-B750-6C1B7EC1EE35"),
                    new Guid("A313DB64-3473-4D49-93F1-6C5773EF4612"),
                    new Guid("D6BE167E-E069-4872-AC26-7FC276C59CFA"),
                    new Guid("86E7D155-4AD7-4D8F-A470-8206F7447B2B"),
                    new Guid("5A95E669-1ADD-4C49-9E29-86293180B1B5"),
                    new Guid("7267E6ED-80D1-480A-87DF-875177822260"),
                    new Guid("AD968BCF-97FD-442E-A5FD-A56753BBC93E"),
                    new Guid("94D08225-1556-4A48-88C9-AF78A642F84D"),
                    new Guid("676EB523-44DA-48F5-BCBE-B2EBD6BB7E6F"),
                    new Guid("DBEB90C2-03C5-4E77-8477-BAD52407872A"),
                    new Guid("5DD310B0-1D6D-475B-9FC6-D1DE901C82EA"),
                    new Guid("D60F0DEE-A8E9-4177-8956-D94D378969ED"),
                    new Guid("1141DB2F-7190-47B7-A9B3-E1B831329D5E"),
                    new Guid("259D060F-8E14-4AE9-8D86-FE4B99D7EE77")
                });

            migrationBuilder.DropTable(
                name: "Drinks");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "LobbyEvents");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "UserBanners");

            migrationBuilder.DropTable(
                name: "UserRefreshTokens");

            migrationBuilder.DropTable(
                name: "UserTitles");

            migrationBuilder.DropTable(
                name: "LobbyMemberPicks");

            migrationBuilder.DropTable(
                name: "Banners");

            migrationBuilder.DropTable(
                name: "Titles");

            migrationBuilder.DropTable(
                name: "LobbyMembers");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "Lobbies");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
