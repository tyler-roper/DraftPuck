using DraftPuck.Domain.Achievements;
using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies.Recap;

namespace DraftPuck.Tests.Application.Lobbies;

[Collection(DatabaseCollection.Name)]
public class LobbyRecapTests(DatabaseFixture fixture)
{
    private int _eventId;

    private DrinkEntity Drink(LobbyMemberPickEntity pick) => new()
    {
        Id = Guid.NewGuid(),
        LobbyMemberPickId = pick.Id,
        EventId = ++_eventId,
        Created = DateTime.UtcNow
    };

    private async Task<LobbyRecapDto> RecapAsync(string code, IGameCache cache, INhlClient? nhl = null)
    {
        await using var db = fixture.CreateContext();
        return await new GetLobbyRecapQueryHandler(db, cache, nhl ?? Substitute.For<INhlClient>(), NullLogger<GetLobbyRecapQueryHandler>.Instance)
            .Handle(new GetLobbyRecapQuery { Code = code }, default);
    }

    [Fact]
    public async Task TheRecapDescribesPicksChatAndAchievementsFromTheNight()
    {
        var gameId = Seed.NewGameId();
        // Puck drop is after the picks, so nobody's a buzzer beater.
        var game = TestGames.Create(gameId, DateTime.UtcNow.AddHours(1));
        var star = game.HomeTeam.Roster[0];
        star.Goals = 20;
        var sleeper = game.AwayTeam.Roster[1];
        sleeper.Goals = 0;
        var cache = new InMemoryGameCache();
        await cache.AddGameAsync(game);

        string code;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, sam) = seed.Lobby([gameId], isActive: false);
            lobby.Created = DateTime.UtcNow.AddHours(-4);
            sam.Name = "Sam";
            var alex = seed.Member(lobby);
            alex.Name = "Alex";
            var jo = seed.Member(lobby);
            jo.Name = "Jo";

            var samPick = seed.Pick(sam, gameId, TestGames.HomeTeamId, star.Id);
            db.Drinks.AddRange(Drink(samPick), Drink(samPick), Drink(samPick));
            var joPick = seed.Pick(jo, gameId, TestGames.AwayTeamId, sleeper.Id);
            db.Drinks.Add(Drink(joPick));

            for (var i = 0; i < 30; i++)
                db.Messages.Add(new MessageEntity { Id = Guid.NewGuid(), LobbyMemberId = alex.Id, Message = $"chirp {i}", Sent = DateTime.UtcNow });

            // A brand-new achievement only Alex has, earned that night; one earned two days later doesn't count.
            var rare = new AchievementEntity { Id = Guid.NewGuid(), UniqueIdentifier = $"recap-{Guid.NewGuid():N}", FriendlyName = "Back-to-Back", Description = "Play three nights in a row" };
            var later = new AchievementEntity { Id = Guid.NewGuid(), UniqueIdentifier = $"recap-{Guid.NewGuid():N}", FriendlyName = "Later", Description = "Too late" };
            db.Achievements.AddRange(rare, later);
            db.UserAchievements.AddRange(
                new UserAchievementEntity { Id = Guid.NewGuid(), UserId = alex.UserId, AchievementId = rare.Id, DateEarned = lobby.Created.AddHours(3) },
                new UserAchievementEntity { Id = Guid.NewGuid(), UserId = jo.UserId, AchievementId = later.Id, DateEarned = lobby.Created.AddDays(2) });
            // Plenty of other players with achievements, so Alex's is rare.
            for (var i = 0; i < 20; i++)
                db.UserAchievements.Add(new UserAchievementEntity { Id = Guid.NewGuid(), UserId = seed.User().Id, AchievementId = later.Id, DateEarned = DateTime.UtcNow });

            await db.SaveChangesAsync();
            code = lobby.JoinCode;
        }

        var awards = (await RecapAsync(code, cache)).Awards.ToDictionary(a => a.Key);

        Assert.Equal(["best_pick", "chatterbox", "over_achiever", "sleeper"], awards.Keys.Order());
        Assert.Equal(("Best pick", "Sam", $"{star.LastName} (HOM) earned 3 drinks"),
            (awards["best_pick"].Title, awards["best_pick"].MemberName, awards["best_pick"].Detail));
        Assert.Equal(("Jo", $"{sleeper.LastName} (AWY) scored with 0 goals on the season"), (awards["sleeper"].MemberName, awards["sleeper"].Detail));
        Assert.Equal(("Alex", "30 messages in the chat"), (awards["chatterbox"].MemberName, awards["chatterbox"].Detail));
        Assert.Equal("Alex", awards["over_achiever"].MemberName);
        Assert.StartsWith("Earned \"Back-to-Back\": Play three nights in a row (", awards["over_achiever"].Detail);
    }

    [Fact]
    public async Task AGameThatCannotBeLoadedStillGivesARecap()
    {
        var gameId = Seed.NewGameId();
        var nhl = Substitute.For<INhlClient>();
        nhl.GetFullGameAsync(gameId).Returns<GameDto>(_ => throw new HttpRequestException("NHL API down"));

        string code;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, host) = seed.Lobby([gameId]);
            var pick = seed.Pick(host, gameId, TestGames.HomeTeamId, TestGames.HomePlayer(0));
            db.Drinks.Add(Drink(pick));
            await db.SaveChangesAsync();
            code = lobby.JoinCode;
        }

        var best = Assert.Single((await RecapAsync(code, new InMemoryGameCache(), nhl)).Awards);
        Assert.Equal("a player earned 1 drink", best.Detail);
    }
}
