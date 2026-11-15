using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftPuck.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreAchievements : Migration
    {
        // Twenty achievements, each with a title. Banners are added as their art is made.
        private static readonly string[] Identifiers = ["sudden_death", "buzzer_beater", "first_blood", "game_winner", "man_advantage", "empty_netter", "late_call_up", "homer", "clean_sheet", "healthy_scratch", "goose_egg", "designated_driver", "target_practice", "spread_the_wealth", "bottomless_cup", "back_to_back", "season_ticket_holder", "chirp_machine", "bot_army", "playoff_beard"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "UniqueIdentifier", "FriendlyName", "Description" },
                values: new object[,]
                {
                    { new Guid("9d0f22f6-da6f-4148-bf68-1bd1d362bbcd"), "sudden_death", "Sudden Death", "Have one of your players score in overtime." },
                    { new Guid("6b3cc388-af10-427c-8b6e-1d961bc70316"), "buzzer_beater", "Buzzer Beater", "Have one of your players score with under 10 seconds left in a period." },
                    { new Guid("6e8d0a9a-b67e-4e3c-b55e-f851e068d493"), "first_blood", "First Blood", "Have one of your players score the first goal of a game." },
                    { new Guid("dd3dfaec-9f7d-48f1-b886-0812ecce224a"), "game_winner", "Game Winner", "Have one of your players score the game-winning goal." },
                    { new Guid("c8cf8a84-b3dc-4619-8be2-945a3bdc50d0"), "man_advantage", "Man Advantage", "Have one of your players score a power-play goal." },
                    { new Guid("f30dc0ed-a02e-4117-9b3e-9b8ff9532e43"), "empty_netter", "Empty Netter", "Have one of your players score into an empty net." },
                    { new Guid("aecf5fa0-ebad-4feb-9da5-13c63437a928"), "late_call_up", "Late Call-Up", "Pick a player after puck drop, and have them score." },
                    { new Guid("d4cf5e98-613f-4e88-9c97-f5c4fd46df30"), "homer", "Homer", "Make every pick in a lobby from the same team, and have at least one of them score." },
                    { new Guid("b80d38f8-9c86-478b-a2b4-d03d79b8fbdd"), "clean_sheet", "Clean Sheet", "Have every one of your picks in a lobby score (at least 3 picks)." },
                    { new Guid("74f8dc70-42e2-4b2f-98e7-619c45e0b56d"), "healthy_scratch", "Healthy Scratch", "Have one of your picks scratched from the lineup." },
                    { new Guid("0aae2d54-614b-484b-a4d2-570d416c4310"), "goose_egg", "Goose Egg", "Make 4 or more picks in a lobby and have none of them score." },
                    { new Guid("80804a8b-7db6-4f30-8c2a-b0ed2a1077ae"), "designated_driver", "Designated Driver", "Finish a lobby with at least 5 human players without receiving a single drink." },
                    { new Guid("488028f9-32a3-4f7f-9aed-48ec07141e69"), "target_practice", "Target Practice", "Give 5 drinks to the same person in one lobby." },
                    { new Guid("6876ca5e-97ca-4cef-bcad-268b89ee03be"), "spread_the_wealth", "Spread the Wealth", "Give drinks to 5 different people in one lobby." },
                    { new Guid("cde72674-848e-4d2a-b365-13422fe4df45"), "bottomless_cup", "Bottomless Cup", "Receive 10 or more drinks in one lobby." },
                    { new Guid("23ecfc6d-7e63-4648-b5fc-12ddb46dc66f"), "back_to_back", "Back-to-Back", "Play in lobbies on 3 nights in a row." },
                    { new Guid("b1ebc676-9c86-4a49-8950-09d5983a338b"), "season_ticket_holder", "Season Ticket Holder", "Play in lobbies on 25 different nights." },
                    { new Guid("1f61ba29-e51e-4946-93b3-a6429b1213f4"), "chirp_machine", "Chirp Machine", "Send 50 chat messages in one lobby." },
                    { new Guid("f18cce4d-b756-42f8-915a-f4f28651e828"), "bot_army", "Bot Army", "Host a lobby with the maximum number of bots." },
                    { new Guid("c052da50-887d-40ec-8c15-211eac60af7b"), "playoff_beard", "Playoff Beard", "Play in a lobby during the playoffs." },
                });

            migrationBuilder.InsertData(
                table: "Titles",
                columns: new[] { "Id", "UniqueIdentifier", "Text", "AchievementId" },
                values: new object[,]
                {
                    { new Guid("1220e158-1310-469d-bd2c-6f4d011ad1db"), "sudden_death", "Sudden Death", new Guid("9d0f22f6-da6f-4148-bf68-1bd1d362bbcd") },
                    { new Guid("a6f42987-6129-465e-91cb-31bf5072a0fc"), "buzzer_beater", "Buzzer Beater", new Guid("6b3cc388-af10-427c-8b6e-1d961bc70316") },
                    { new Guid("3c2375ea-a735-46b3-a56a-f3f31fc7d960"), "first_blood", "First Blood", new Guid("6e8d0a9a-b67e-4e3c-b55e-f851e068d493") },
                    { new Guid("15d09575-dbd3-41b5-93da-04c01a21cf55"), "game_winner", "Game Winner", new Guid("dd3dfaec-9f7d-48f1-b886-0812ecce224a") },
                    { new Guid("064ce200-9e9e-4429-b9a7-c366eee07c98"), "man_advantage", "Man Advantage", new Guid("c8cf8a84-b3dc-4619-8be2-945a3bdc50d0") },
                    { new Guid("196b634e-53d1-4c2d-93d5-e460ff0ca721"), "empty_netter", "Empty Netter", new Guid("f30dc0ed-a02e-4117-9b3e-9b8ff9532e43") },
                    { new Guid("2339f387-5ad9-4ae2-87b9-2f5e42ec4a3d"), "late_call_up", "Late Call-Up", new Guid("aecf5fa0-ebad-4feb-9da5-13c63437a928") },
                    { new Guid("0a549c87-97f5-4da1-bc5e-5d08c8901037"), "homer", "Homer", new Guid("d4cf5e98-613f-4e88-9c97-f5c4fd46df30") },
                    { new Guid("920bd64a-038c-4226-8476-3b0087c98e16"), "clean_sheet", "Clean Sheet", new Guid("b80d38f8-9c86-478b-a2b4-d03d79b8fbdd") },
                    { new Guid("8df356ed-713c-46d3-afbc-ad4f2204259a"), "healthy_scratch", "Healthy Scratch", new Guid("74f8dc70-42e2-4b2f-98e7-619c45e0b56d") },
                    { new Guid("9b5876d8-3e6b-4646-80cb-c03d2376351d"), "goose_egg", "Goose Egg", new Guid("0aae2d54-614b-484b-a4d2-570d416c4310") },
                    { new Guid("8b9c8f07-cf18-4691-8ce1-2c4a3b6644b4"), "designated_driver", "Designated Driver", new Guid("80804a8b-7db6-4f30-8c2a-b0ed2a1077ae") },
                    { new Guid("3f316972-72f4-46df-8e3e-9383291350f0"), "target_practice", "Target Practice", new Guid("488028f9-32a3-4f7f-9aed-48ec07141e69") },
                    { new Guid("40aee143-7ac9-4867-b670-2fbf10d42909"), "spread_the_wealth", "Spread the Wealth", new Guid("6876ca5e-97ca-4cef-bcad-268b89ee03be") },
                    { new Guid("3d1169ac-43fa-4a93-b74d-4c45ec006c03"), "bottomless_cup", "Bottomless Cup", new Guid("cde72674-848e-4d2a-b365-13422fe4df45") },
                    { new Guid("e521052e-3a7d-4eab-84c0-313b097307f2"), "back_to_back", "Back-to-Back", new Guid("23ecfc6d-7e63-4648-b5fc-12ddb46dc66f") },
                    { new Guid("ef3067e5-9cb9-406d-8403-e8b2edd9b3f6"), "season_ticket_holder", "Season Ticket Holder", new Guid("b1ebc676-9c86-4a49-8950-09d5983a338b") },
                    { new Guid("af2b244b-6ccb-4e03-a349-5e2020cd3ca0"), "chirp_machine", "Chirp Machine", new Guid("1f61ba29-e51e-4946-93b3-a6429b1213f4") },
                    { new Guid("b9b026b0-5c85-4678-abb0-26f0bcc87485"), "bot_army", "Bot Army", new Guid("f18cce4d-b756-42f8-915a-f4f28651e828") },
                    { new Guid("0a355fa2-55d4-4629-aa5f-9b5c20129491"), "playoff_beard", "Playoff Beard", new Guid("c052da50-887d-40ec-8c15-211eac60af7b") },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var list = string.Join(", ", Identifiers.Select(id => $"'{id}'"));
            migrationBuilder.Sql($"""
                DELETE ut FROM UserTitles ut INNER JOIN Titles t ON t.Id = ut.TitleId WHERE t.UniqueIdentifier IN ({list});
                DELETE ua FROM UserAchievements ua INNER JOIN Achievements a ON a.Id = ua.AchievementId WHERE a.UniqueIdentifier IN ({list});
                DELETE FROM Titles WHERE UniqueIdentifier IN ({list});
                DELETE FROM Achievements WHERE UniqueIdentifier IN ({list});
                """);
        }
    }
}