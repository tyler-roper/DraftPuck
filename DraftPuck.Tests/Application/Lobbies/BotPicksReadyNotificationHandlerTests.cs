using DraftPuck.Application.Features.Lobbies.Picks;

namespace DraftPuck.Tests.Application.Lobbies;

[Collection(DatabaseCollection.Name)]
public class BotPicksReadyNotificationHandlerTests(DatabaseFixture fixture)
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private async Task RunAsync(GameDto game)
    {
        await using var db = fixture.CreateContext();
        await new BotPicksReadyNotificationHandler(db, _mediator).Handle(new BotPicksReadyNotification(game), default);
    }

    private async Task<List<LobbyMemberPickEntity>> PicksForAsync(Guid memberId, int gameId)
    {
        await using var db = fixture.CreateContext();
        return await db.LobbyMemberPicks.Where(p => p.LobbyMemberId == memberId && p.GameId == gameId && p.IsActive).ToListAsync();
    }

    [Fact]
    public async Task FillsEachBotsPicksForBothTeams()
    {
        var gameId = Seed.NewGameId();
        Guid botId;
        string code;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, _) = seed.Lobby([gameId], picksPerTeam: 2);
            botId = seed.Bot(lobby).Id;
            code = lobby.JoinCode;
            await db.SaveChangesAsync();
        }

        await RunAsync(TestGames.Create(gameId, DateTime.UtcNow.AddMinutes(20)));

        var picks = await PicksForAsync(botId, gameId);
        Assert.Equal(2, picks.Count(p => p.TeamId == TestGames.HomeTeamId));
        Assert.Equal(2, picks.Count(p => p.TeamId == TestGames.AwayTeamId));
        await _mediator.Received(1).Publish(
            Arg.Is<LobbyStateChangedNotification>(n => n.Data.LobbyJoinCode == code), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnlyFillsRemainingPicksAndSkipsPlayersAlreadyTaken()
    {
        var gameId = Seed.NewGameId();
        Guid botId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, host) = seed.Lobby([gameId]);
            var bot = seed.Bot(lobby, BotPickStyle.Best);
            botId = bot.Id;
            seed.Pick(host, gameId, TestGames.HomeTeamId, TestGames.HomePlayer(0)); // the human took the best home player
            seed.Pick(bot, gameId, TestGames.AwayTeamId, TestGames.AwayPlayer(3)); // the bot already has its away pick
            await db.SaveChangesAsync();
        }

        await RunAsync(TestGames.Create(gameId, DateTime.UtcNow.AddMinutes(20)));

        var picks = await PicksForAsync(botId, gameId);
        Assert.Equal(2, picks.Count);
        Assert.Contains(picks, p => p.PlayerId == TestGames.HomePlayer(1)); // best home player still available
        Assert.Contains(picks, p => p.PlayerId == TestGames.AwayPlayer(3)); // untouched
    }

    [Fact]
    public async Task BotsInTheSameLobbyNeverPickTheSamePlayer()
    {
        var gameId = Seed.NewGameId();
        Guid lobbyId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, _) = seed.Lobby([gameId], picksPerTeam: 2);
            lobbyId = lobby.Id;
            for (var i = 0; i < 3; i++) seed.Bot(lobby, BotPickStyle.Best);
            await db.SaveChangesAsync();
        }

        await RunAsync(TestGames.Create(gameId, DateTime.UtcNow.AddMinutes(20)));

        await using var check = fixture.CreateContext();
        var playerIds = await check.LobbyMemberPicks
            .Where(p => p.LobbyMember.LobbyId == lobbyId && p.GameId == gameId)
            .Select(p => p.PlayerId)
            .ToListAsync();
        Assert.Equal(12, playerIds.Count);
        Assert.Equal(playerIds.Count, playerIds.Distinct().Count());
    }

    [Fact]
    public async Task DoesNothingOnceEveryBotIsFull()
    {
        var gameId = Seed.NewGameId();
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, _) = seed.Lobby([gameId]);
            var bot = seed.Bot(lobby);
            seed.Pick(bot, gameId, TestGames.HomeTeamId, TestGames.HomePlayer(0));
            seed.Pick(bot, gameId, TestGames.AwayTeamId, TestGames.AwayPlayer(0));
            await db.SaveChangesAsync();
        }

        await RunAsync(TestGames.Create(gameId, DateTime.UtcNow.AddMinutes(20)));

        await _mediator.DidNotReceiveWithAnyArgs().Publish<LobbyStateChangedNotification>(default!, default);
    }

    [Fact]
    public async Task IgnoresRemovedBotsInactiveLobbiesOtherGamesAndManualPickingLobbies()
    {
        var gameId = Seed.NewGameId();
        Guid removedBotId, inactiveLobbyBotId, otherGameBotId, manualLobbyBotId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);

            var (lobby, _) = seed.Lobby([gameId]);
            var removedBot = seed.Bot(lobby);
            removedBot.IsRemoved = true;
            removedBotId = removedBot.Id;

            var (inactiveLobby, _) = seed.Lobby([gameId], isActive: false);
            inactiveLobbyBotId = seed.Bot(inactiveLobby).Id;

            var (otherGameLobby, _) = seed.Lobby([Seed.NewGameId()]);
            otherGameBotId = seed.Bot(otherGameLobby).Id;

            // The host turned auto-picking off and picks for the bots.
            var (manualLobby, _) = seed.Lobby([gameId], botAutoPicking: false);
            manualLobbyBotId = seed.Bot(manualLobby).Id;

            await db.SaveChangesAsync();
        }

        await RunAsync(TestGames.Create(gameId, DateTime.UtcNow.AddMinutes(20)));

        Assert.Empty(await PicksForAsync(removedBotId, gameId));
        Assert.Empty(await PicksForAsync(inactiveLobbyBotId, gameId));
        Assert.Empty(await PicksForAsync(otherGameBotId, gameId));
        Assert.Empty(await PicksForAsync(manualLobbyBotId, gameId));
    }

    [Fact]
    public async Task BotWithoutPickStyleStillPicks()
    {
        var gameId = Seed.NewGameId();
        Guid botId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, _) = seed.Lobby([gameId]);
            botId = seed.Bot(lobby, style: null).Id;
            await db.SaveChangesAsync();
        }

        await RunAsync(TestGames.Create(gameId, DateTime.UtcNow.AddMinutes(20)));

        Assert.Equal(2, (await PicksForAsync(botId, gameId)).Count);
    }

    [Theory]
    [InlineData(BotPickStyle.Best, 0, 0)]
    [InlineData(BotPickStyle.Worst, 7, 7)]
    [InlineData(BotPickStyle.Good, 0, 5)]
    [InlineData(BotPickStyle.Average, 5, 7)] // range 5-10, cut short by an 8-player roster
    [InlineData(BotPickStyle.Bad, 2, 7)]     // the 6 worst of 8
    public async Task PicksFromTheStylesRange(BotPickStyle style, int bestRank, int worstRank)
    {
        var gameId = Seed.NewGameId();
        Guid botId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, _) = seed.Lobby([gameId]);
            botId = seed.Bot(lobby, style).Id;
            await db.SaveChangesAsync();
        }

        await RunAsync(TestGames.Create(gameId, DateTime.UtcNow.AddMinutes(20), playersPerTeam: 8));

        var homePick = (await PicksForAsync(botId, gameId)).Single(p => p.TeamId == TestGames.HomeTeamId);
        var rank = homePick.PlayerId - TestGames.HomePlayer(0);
        Assert.InRange(rank, bestRank, worstRank);
    }

    [Fact]
    public async Task AverageBotFallsBackWhenItsRangeIsEmpty()
    {
        // Only 3 players dressed: nothing in the Average range (ranks 5-10), so it takes the fallback instead of failing.
        var gameId = Seed.NewGameId();
        Guid botId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, _) = seed.Lobby([gameId]);
            botId = seed.Bot(lobby, BotPickStyle.Average).Id;
            await db.SaveChangesAsync();
        }

        await RunAsync(TestGames.Create(gameId, DateTime.UtcNow.AddMinutes(20), playersPerTeam: 3));

        var homePick = (await PicksForAsync(botId, gameId)).Single(p => p.TeamId == TestGames.HomeTeamId);
        Assert.Equal(TestGames.HomePlayer(2), homePick.PlayerId); // the worst available, closest to the range
    }
}
