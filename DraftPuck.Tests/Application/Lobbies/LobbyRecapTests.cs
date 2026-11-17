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

    private async Task<LobbyRecapDto> RecapAsync(string code, IGameCache cache, INhlClient? nhl = null, ILobbyRecapCache? recapCache = null)
    {
        await using var db = fixture.CreateContext();
        return await new GetLobbyRecapQueryHandler(db, cache, nhl ?? Substitute.For<INhlClient>(), recapCache ?? new InMemoryLobbyRecapCache(),
                NullLogger<GetLobbyRecapQueryHandler>.Instance)
            .Handle(new GetLobbyRecapQuery { Code = code }, default);
    }

    /// <summary>A lobby whose host's pick (in a cached game) earned one drink.</summary>
    private async Task<(Guid LobbyId, string Code, LobbyMemberPickEntity Pick)> OneDrinkLobbyAsync(int gameId, bool isActive, DateTime created)
    {
        await using var db = fixture.CreateContext();
        var seed = new Seed(db);
        var (lobby, host) = seed.Lobby([gameId], isActive: isActive);
        lobby.Created = created;
        var pick = seed.Pick(host, gameId, TestGames.HomeTeamId, TestGames.HomePlayer(0));
        db.Drinks.Add(Drink(pick));
        await db.SaveChangesAsync();
        return (lobby.Id, lobby.JoinCode, pick);
    }

    private async Task AddDrinkAsync(LobbyMemberPickEntity pick)
    {
        await using var db = fixture.CreateContext();
        db.Drinks.Add(Drink(pick));
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(2, 30 * 24 * 60)] // ended days ago: cached for weeks
    [InlineData(0, 10)] // still its night: achievements can arrive, so only briefly
    public async Task AFinishedLobbysRecapIsCached(int daysAgo, int expectedMinutes)
    {
        var gameId = Seed.NewGameId();
        var games = new InMemoryGameCache();
        // Puck drop after the pick, so it's not a buzzer beater.
        await games.AddGameAsync(TestGames.Create(gameId, DateTime.UtcNow.AddHours(2), GameState.Final));
        var (lobbyId, code, pick) = await OneDrinkLobbyAsync(gameId, isActive: false, DateTime.UtcNow.AddDays(-daysAgo).AddHours(-1));
        var recapCache = new InMemoryLobbyRecapCache();

        var first = await RecapAsync(code, games, recapCache: recapCache);
        await AddDrinkAsync(pick);
        var second = await RecapAsync(code, games, recapCache: recapCache);

        Assert.EndsWith("earned 1 drink", Assert.Single(second.Awards).Detail); // the cached recap, not the new drink
        Assert.Equal(first.Awards.Single().Detail, second.Awards.Single().Detail);
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), recapCache.Expiries[lobbyId]);
    }

    [Fact]
    public async Task AnActiveLobbysRecapIsNotCached()
    {
        var gameId = Seed.NewGameId();
        var games = new InMemoryGameCache();
        await games.AddGameAsync(TestGames.Create(gameId, DateTime.UtcNow.AddHours(2), GameState.Live));
        var (lobbyId, code, pick) = await OneDrinkLobbyAsync(gameId, isActive: true, DateTime.UtcNow.AddHours(-1));
        var recapCache = new InMemoryLobbyRecapCache();

        await RecapAsync(code, games, recapCache: recapCache);
        await AddDrinkAsync(pick);

        Assert.EndsWith("earned 2 drinks", Assert.Single((await RecapAsync(code, games, recapCache: recapCache)).Awards).Detail);
        Assert.Empty(recapCache.Expiries);
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
    public async Task TheRecapNamesEveryPickedPlayer()
    {
        var gameId = Seed.NewGameId();
        var game = TestGames.Create(gameId, DateTime.UtcNow.AddHours(1));
        var cache = new InMemoryGameCache();
        await cache.AddGameAsync(game);
        var home = game.PlayerSummaries.First(p => p.Id == TestGames.HomePlayer(0));
        var away = game.PlayerSummaries.First(p => p.Id == TestGames.AwayPlayer(0));

        string code;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, sam) = seed.Lobby([gameId]);
            var alex = seed.Member(lobby);
            seed.Pick(sam, gameId, TestGames.HomeTeamId, home.Id);
            seed.Pick(alex, gameId, TestGames.AwayTeamId, away.Id);
            await db.SaveChangesAsync();
            code = lobby.JoinCode;
        }

        var players = (await RecapAsync(code, cache)).Players.OrderBy(p => p.Team).Select(p => (p.GameId, p.PlayerId, p.Name, p.Team));

        Assert.Equal([(gameId, away.Id, $"{away.FirstName} {away.LastName}", "AWY"), (gameId, home.Id, $"{home.FirstName} {home.LastName}", "HOM")], players);
    }

    [Fact]
    public async Task ARivalryNamesBothMembersAndOtherAwardsOnlyTheirWinner()
    {
        var gameId = Seed.NewGameId();
        var cache = new InMemoryGameCache();
        await cache.AddGameAsync(TestGames.Create(gameId, DateTime.UtcNow.AddHours(1)));

        string code;
        Guid samId, alexId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, sam) = seed.Lobby([gameId]);
            var alex = seed.Member(lobby);
            (samId, alexId) = (sam.Id, alex.Id);
            // Four drinks each way: a rivalry strong enough to always make the recap.
            var samPick = seed.Pick(sam, gameId, TestGames.HomeTeamId, TestGames.HomePlayer(0));
            var alexPick = seed.Pick(alex, gameId, TestGames.AwayTeamId, TestGames.AwayPlayer(0));
            for (var i = 0; i < 4; i++)
            {
                var toAlex = Drink(samPick);
                toAlex.RecipientLobbyMemberId = alex.Id;
                var toSam = Drink(alexPick);
                toSam.RecipientLobbyMemberId = sam.Id;
                db.Drinks.AddRange(toAlex, toSam);
            }
            await db.SaveChangesAsync();
            code = lobby.JoinCode;
        }

        var awards = (await RecapAsync(code, cache)).Awards;

        var rivalry = Assert.Single(awards, a => a.Key == LobbyAwards.Rivalry);
        Assert.Equal(new[] { samId, alexId }.Order(), new[] { rivalry.MemberId, rivalry.OtherMemberId!.Value }.Order());
        Assert.All(awards.Where(a => a.Key != LobbyAwards.Rivalry), a => Assert.Null(a.OtherMemberId));
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
            var (lobby, host) = seed.Lobby([gameId], isActive: false);
            lobby.Created = DateTime.UtcNow.AddDays(-3);
            var pick = seed.Pick(host, gameId, TestGames.HomeTeamId, TestGames.HomePlayer(0));
            db.Drinks.Add(Drink(pick));
            await db.SaveChangesAsync();
            code = lobby.JoinCode;
        }

        var recapCache = new InMemoryLobbyRecapCache();
        var best = Assert.Single((await RecapAsync(code, new InMemoryGameCache(), nhl, recapCache)).Awards);
        Assert.Equal("a player earned 1 drink", best.Detail);
        Assert.Empty(recapCache.Expiries); // not cached: the next view tries the game again
    }
}
