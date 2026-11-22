using DraftPuck.Application.Features.Games;

namespace DraftPuck.Tests.Application.Games;

/// <summary>When a goal the feed stops listing is kept, and when it's let go. The lobby side is in GoalCorrectionPipelineTests.</summary>
public class GoalFeedGapsTests
{
    private static readonly int Scorer = TestGames.HomePlayer(1);
    private static readonly int OtherScorer = TestGames.HomePlayer(2);

    private static GameDto Game(int homeScore, params PlayDto[] plays)
    {
        var game = TestGames.Create(1, DateTime.UtcNow, GameState.Live);
        game.Plays = [.. plays];
        game.HomeTeam.Score = homeScore;
        return game;
    }

    private static PlayDto Goal(int id, string time, int scorer = 0) => TestGames.Goal(id, 1, time, scorer == 0 ? Scorer : scorer, TestGames.HomeTeamId);

    private static PlayDto Shot(int id, string time) => new() { Id = id, Period = 1, TimeInPeriod = time, Type = PlayType.ShotOnGoal };

    [Fact]
    public void KeepsAGoalTheFeedDroppedWhileTheScoreCountsIt_InItsPlace()
    {
        var previous = Game(1, Shot(1, "02:00"), Goal(2, "05:00"), Shot(3, "08:00"));
        var game = Game(1, Shot(1, "02:00"), Shot(3, "08:00"));

        var restored = GoalFeedGaps.Restore(previous, game);

        Assert.Equal(2, Assert.Single(restored).Id);
        Assert.Equal([1, 2, 3], game.Plays.Select(p => p.Id));
    }

    [Fact]
    public void LetsTheGoalGoOnceTheScoreDrops()
    {
        var previous = Game(1, Goal(2, "05:00"));
        var game = Game(0);

        Assert.Empty(GoalFeedGaps.Restore(previous, game));
        Assert.Empty(game.Plays);
    }

    [Fact]
    public void KeepsTheScorerOfAGoalThatLostIt()
    {
        var previous = Game(1, Goal(2, "05:00"));
        var noScorer = Goal(2, "05:00");
        noScorer.PrimaryPlayerId = null;
        var game = Game(1, noScorer);

        GoalFeedGaps.Restore(previous, game);

        Assert.Equal(Scorer, Assert.Single(game.Plays).PrimaryPlayerId);
    }

    [Fact]
    public void LeavesAReassignedGoalAlone()
    {
        var previous = Game(1, Goal(2, "05:00"));
        var game = Game(1, Goal(2, "05:00", OtherScorer));

        Assert.Empty(GoalFeedGaps.Restore(previous, game));
        Assert.Equal(OtherScorer, Assert.Single(game.Plays).PrimaryPlayerId);
    }

    [Fact]
    public void AGoalCalledBackAsAnotherIsScoredIsNotKept()
    {
        // Same score as before, but the feed lists as many goals as the score: the old one really is gone.
        var previous = Game(1, Goal(2, "05:00"));
        var game = Game(1, Goal(9, "12:00", OtherScorer));

        Assert.Empty(GoalFeedGaps.Restore(previous, game));
    }

    [Fact]
    public void ARenumberedGoalIsNotKeptTwice()
    {
        var previous = Game(1, Goal(2, "05:00"));
        var game = Game(1, Goal(3, "05:00"));

        Assert.Empty(GoalFeedGaps.Restore(previous, game));
    }

    [Fact]
    public void FirstPollHasNothingToKeep()
        => Assert.Empty(GoalFeedGaps.Restore(null, Game(1)));
}
