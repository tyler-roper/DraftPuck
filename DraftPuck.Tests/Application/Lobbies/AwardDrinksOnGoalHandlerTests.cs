using DraftPuck.Application.Features.Lobbies.Drinks;

namespace DraftPuck.Tests.Application.Lobbies;

[Collection(DatabaseCollection.Name)]
public class AwardDrinksOnGoalHandlerTests(DatabaseFixture fixture)
{
    private const int PlayId = 55;
    private static readonly int Scorer = TestGames.HomePlayer(0);

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private static GoalScoredNotification GoalBy(int gameId, int scorerId) => new(new GoalEventPayload(
        gameId,
        TestGames.Goal(PlayId, 1, "05:00", scorerId, TestGames.HomeTeamId),
        new PlayerDto { Id = scorerId, TeamId = TestGames.HomeTeamId }));

    /// <summary>Runs the handler, with bot assignments going through the real AssignDrinkCommandHandler.</summary>
    private async Task RunAsync(GoalScoredNotification notification)
    {
        await using var db = fixture.CreateContext();
        _mediator.Send(Arg.Any<AssignDrinkCommand>(), Arg.Any<CancellationToken>())
            .Returns(ci => new AssignDrinkCommandHandler(db, Substitute.For<AutoMapper.IMapper>(), _mediator)
                .Handle(ci.Arg<AssignDrinkCommand>(), default));

        await new AwardDrinksOnGoalHandler(db, _mediator, NullLogger<AwardDrinksOnGoalHandler>.Instance)
            .Handle(notification, default);
    }

    private async Task<List<DrinkEntity>> DrinksForPickAsync(Guid pickId)
    {
        await using var db = fixture.CreateContext();
        return await db.Drinks.Where(d => d.LobbyMemberPickId == pickId).ToListAsync();
    }

    [Fact]
    public async Task AwardsOneUnassignedDrinkToAHumanWhosePlayerScored()
    {
        var gameId = Seed.NewGameId();
        Guid pickId, otherPickId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, host) = seed.Lobby([gameId]);
            pickId = seed.Pick(host, gameId, TestGames.HomeTeamId, Scorer).Id;
            otherPickId = seed.Pick(seed.Member(lobby), gameId, TestGames.HomeTeamId, TestGames.HomePlayer(1)).Id;
            await db.SaveChangesAsync();
        }

        await RunAsync(GoalBy(gameId, Scorer));

        var drink = Assert.Single(await DrinksForPickAsync(pickId));
        Assert.Equal(PlayId, drink.EventId);
        Assert.Null(drink.RecipientLobbyMemberId); // humans hand out their own drinks
        Assert.Empty(await DrinksForPickAsync(otherPickId));
        await _mediator.Received(1).Publish(Arg.Any<DrinkAwardedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoesNotAwardTheSameGoalTwice()
    {
        var gameId = Seed.NewGameId();
        Guid pickId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (_, host) = seed.Lobby([gameId]);
            pickId = seed.Pick(host, gameId, TestGames.HomeTeamId, Scorer).Id;
            await db.SaveChangesAsync();
        }

        await RunAsync(GoalBy(gameId, Scorer));
        await RunAsync(GoalBy(gameId, Scorer));

        Assert.Single(await DrinksForPickAsync(pickId));
    }

    [Fact]
    public async Task BotHandsItsDrinkToAHumanInItsLobby()
    {
        var gameId = Seed.NewGameId();
        Guid botPickId, hostId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, host) = seed.Lobby([gameId]);
            hostId = host.Id;
            botPickId = seed.Pick(seed.Bot(lobby), gameId, TestGames.HomeTeamId, Scorer).Id;
            await db.SaveChangesAsync();
        }

        await RunAsync(GoalBy(gameId, Scorer));

        var drink = Assert.Single(await DrinksForPickAsync(botPickId));
        Assert.Equal(hostId, drink.RecipientLobbyMemberId);
    }

    [Fact]
    public async Task BotWithNoRecipientsDoesNotShiftOtherBotsDrinks()
    {
        // Regression: assignments were matched to drinks by position, so a bot with nobody to give a drink to
        // shifted the next bot's assignment onto the wrong drink, and AssignDrink threw "Drink not found".
        var gameId = Seed.NewGameId();
        Guid lonelyBotPickId, botPickId, hostId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);

            var (emptyLobby, emptyLobbyHost) = seed.Lobby([gameId]);
            emptyLobbyHost.IsRemoved = true; // only the bot is left
            lonelyBotPickId = seed.Pick(seed.Bot(emptyLobby), gameId, TestGames.HomeTeamId, Scorer).Id;

            var (lobby, host) = seed.Lobby([gameId]);
            hostId = host.Id;
            botPickId = seed.Pick(seed.Bot(lobby), gameId, TestGames.HomeTeamId, Scorer).Id;

            await db.SaveChangesAsync();
        }

        await RunAsync(GoalBy(gameId, Scorer));

        Assert.Null(Assert.Single(await DrinksForPickAsync(lonelyBotPickId)).RecipientLobbyMemberId);
        Assert.Equal(hostId, Assert.Single(await DrinksForPickAsync(botPickId)).RecipientLobbyMemberId);
    }

    [Fact]
    public async Task RemovedBotGetsItsDrinkButDoesNotAssignIt()
    {
        var gameId = Seed.NewGameId();
        Guid botPickId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, _) = seed.Lobby([gameId]);
            var bot = seed.Bot(lobby);
            bot.IsRemoved = true;
            botPickId = seed.Pick(bot, gameId, TestGames.HomeTeamId, Scorer).Id;
            await db.SaveChangesAsync();
        }

        await RunAsync(GoalBy(gameId, Scorer));

        Assert.Single(await DrinksForPickAsync(botPickId));
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<AssignDrinkCommand>(), default);
    }

    [Fact]
    public async Task FailedBotAssignmentDoesNotThrow()
    {
        var gameId = Seed.NewGameId();
        Guid botPickId;
        await using (var db = fixture.CreateContext())
        {
            var seed = new Seed(db);
            var (lobby, _) = seed.Lobby([gameId]);
            botPickId = seed.Pick(seed.Bot(lobby), gameId, TestGames.HomeTeamId, Scorer).Id;
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            _mediator.Send(Arg.Any<AssignDrinkCommand>(), Arg.Any<CancellationToken>())
                .Returns<Task<DrinkDto>>(_ => throw new InvalidOperationException("boom"));

            await new AwardDrinksOnGoalHandler(db, _mediator, NullLogger<AwardDrinksOnGoalHandler>.Instance)
                .Handle(GoalBy(gameId, Scorer), default);
        }

        Assert.Single(await DrinksForPickAsync(botPickId));
    }
}
