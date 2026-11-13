using DraftPuck.Application.Features.Games;
using Microsoft.Extensions.Options;

namespace DraftPuck.Tests.Application.Games;

/// <summary>
/// Runs a single poll of a game: the NHL client returns <c>updated</c>, the cache holds <c>cached</c> (the previous poll),
/// and the test checks which notifications were published. No database is needed while no players leave the lineup.
/// </summary>
public class ProcessGameCommandHandlerTests
{
    private readonly IGameCache _gameCache = Substitute.For<IGameCache>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly INhlClient _nhlClient = Substitute.For<INhlClient>();
    private readonly INhlQueueService _queue = Substitute.For<INhlQueueService>();

    private const int PlayId = 7;
    private static readonly int Scorer = TestGames.HomePlayer(2);

    private async Task PollAsync(GameDto cached, GameDto updated)
    {
        _gameCache.GetGameByIdAsync(updated.Id).Returns(cached);
        _nhlClient.GetFullGameAsync(updated.Id).Returns(updated);

        var handler = new ProcessGameCommandHandler(_gameCache, _mediator, Substitute.For<IDbContext>(), _nhlClient, _queue,
            Options.Create(new ApplicationOptions()), NullLogger<ProcessGameCommandHandler>.Instance);

        await handler.Handle(new ProcessGameCommand(updated.Id, IsInitialPopulation: false), default);
    }

    private List<T> Published<T>() => [.. _mediator.ReceivedCalls()
        .Where(c => c.GetMethodInfo().Name == nameof(IMediator.Publish))
        .Select(c => c.GetArguments()[0])
        .OfType<T>()];

    // Starting "now" keeps the game on the current NHL day (see GameProcessingHelpers.IsGameCurrent).
    private static (GameDto Cached, GameDto Updated) LiveGame(int gameId)
        => (TestGames.Create(gameId, DateTime.UtcNow, GameState.Live), TestGames.Create(gameId, DateTime.UtcNow, GameState.Live));

    [Fact]
    public async Task NewGoalPublishesGoalScored()
    {
        var (cached, updated) = LiveGame(Seed.NewGameId());
        updated.Plays = [TestGames.Goal(PlayId, 1, "05:00", Scorer, TestGames.HomeTeamId)];

        await PollAsync(cached, updated);

        var goal = Assert.Single(Published<GoalScoredNotification>());
        Assert.Equal(Scorer, goal.Data.Scorer.Id);
        Assert.Equal(PlayId, goal.Data.Play.Id);
        Assert.Empty(Published<GoalRemovedNotification>());
    }

    [Fact]
    public async Task SameGoalWithSlightlyCorrectedTimePublishesNothing()
    {
        var (cached, updated) = LiveGame(Seed.NewGameId());
        cached.Plays = [TestGames.Goal(PlayId, 1, "05:00", Scorer, TestGames.HomeTeamId)];
        updated.Plays = [TestGames.Goal(PlayId, 1, "05:02", Scorer, TestGames.HomeTeamId)];

        await PollAsync(cached, updated);

        Assert.Empty(Published<GoalScoredNotification>());
        Assert.Empty(Published<GoalRemovedNotification>());
        Assert.Empty(Published<GoalChangedNotification>());
    }

    [Fact]
    public async Task ScorerCorrectionMovesTheGoalToTheNewScorer()
    {
        var newScorer = TestGames.HomePlayer(3);
        var (cached, updated) = LiveGame(Seed.NewGameId());
        cached.Plays = [TestGames.Goal(PlayId, 1, "05:00", Scorer, TestGames.HomeTeamId)];
        updated.Plays = [TestGames.Goal(PlayId, 1, "05:00", newScorer, TestGames.HomeTeamId)];

        await PollAsync(cached, updated);

        Assert.Equal(newScorer, Assert.Single(Published<GoalScoredNotification>()).Data.Scorer.Id);
        Assert.Equal(Scorer, Assert.Single(Published<GoalRemovedNotification>()).Data.Scorer.Id);
        Assert.Single(Published<GoalChangedNotification>());
    }

    [Fact]
    public async Task DisallowedGoalPublishesGoalRemoved()
    {
        var (cached, updated) = LiveGame(Seed.NewGameId());
        cached.Plays = [TestGames.Goal(PlayId, 1, "05:00", Scorer, TestGames.HomeTeamId)];

        await PollAsync(cached, updated);

        var removed = Assert.Single(Published<GoalRemovedNotification>());
        Assert.Equal(PlayId, removed.Data.EventId);
        Assert.Empty(Published<GoalScoredNotification>());
    }

    [Fact]
    public async Task ShootoutGoalsDoNotCount()
    {
        var (cached, updated) = LiveGame(Seed.NewGameId());
        var shootoutGoal = TestGames.Goal(PlayId, 5, "00:00", Scorer, TestGames.HomeTeamId);
        shootoutGoal.PeriodType = PeriodType.Shootout;
        updated.Plays = [shootoutGoal];

        await PollAsync(cached, updated);

        Assert.Empty(Published<GoalScoredNotification>());
    }

    [Fact]
    public async Task BotPicksStillRunDuringLiveGames()
    {
        // Rosters can complete after puck drop, and bots can be added mid-game.
        var (cached, updated) = LiveGame(Seed.NewGameId());

        await PollAsync(cached, updated);

        Assert.Single(Published<BotPicksReadyNotification>());
    }

    [Fact]
    public async Task BotPickFailureDoesNotBlockPicksReady()
    {
        // A minute out keeps the game on the current NHL day except in the minute before the 10:00 UTC cutoff.
        var gameId = Seed.NewGameId();
        var start = DateTime.UtcNow.AddMinutes(1);
        var cached = TestGames.Create(gameId, start);
        var updated = TestGames.Create(gameId, start);

        _gameCache.TryClaimPreGameAlertAsync(gameId).Returns(true);
        _mediator.Publish(Arg.Any<BotPicksReadyNotification>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("boom"));

        await PollAsync(cached, updated);

        Assert.Single(Published<PicksReadyNotification>());
        // The normal one-minute pregame poll was scheduled, not the two-minute retry after an unhandled error.
        await _queue.Received(1).SendMessageAsync(Arg.Any<ProcessGameMessage>(), TimeSpan.FromMinutes(1), Arg.Any<CancellationToken>());
    }
}
