using DraftPuck.Application.Features.Games;

namespace DraftPuck.Tests.Application.Games;

public class GoalMatcherTests
{
    private static readonly PlayerDto John = new() { Id = TestGames.HomePlayer(2), TeamId = TestGames.HomeTeamId };
    private static readonly PlayerDto Jane = new() { Id = TestGames.HomePlayer(3), TeamId = TestGames.HomeTeamId };

    private static TrackedGoal Goal(int playId, PlayerDto scorer, string time = "05:00", int period = 1)
        => new(TestGames.Goal(playId, period, time, scorer.Id, TestGames.HomeTeamId), scorer);

    [Fact]
    public void UnchangedGoalIsNoChange()
        => Assert.Empty(GoalMatcher.Compare([Goal(450, John)], [Goal(450, John)]));

    [Fact]
    public void NewGoalIsAdded()
        => Assert.Equal(450, Assert.IsType<GoalAdded>(Assert.Single(GoalMatcher.Compare([], [Goal(450, John)]))).Goal.PlayId);

    [Fact]
    public void MissingGoalIsCalledBack()
        => Assert.Equal(450, Assert.IsType<GoalCalledBack>(Assert.Single(GoalMatcher.Compare([Goal(450, John)], []))).Goal.PlayId);

    [Fact]
    public void SameIdWithANewScorerIsAScorerChange()
    {
        var change = Assert.IsType<GoalScorerChanged>(Assert.Single(GoalMatcher.Compare([Goal(450, John)], [Goal(450, Jane)])));
        Assert.Equal(John.Id, change.Before.Scorer.Id);
        Assert.Equal(Jane.Id, change.After.Scorer.Id);
    }

    [Theory]
    [InlineData("05:02")] // within tolerance
    [InlineData("05:30")] // outside tolerance: matched by ID, same scorer
    public void CorrectedTimeWithTheSameScorerIsNoChange(string correctedTime)
        => Assert.Empty(GoalMatcher.Compare([Goal(450, John)], [Goal(450, John, correctedTime)]));

    [Theory]
    [InlineData("05:00")]
    [InlineData("05:03")]
    public void NewIdForTheSameGoalIsARenumber(string time)
    {
        var change = Assert.IsType<GoalRenumbered>(Assert.Single(GoalMatcher.Compare([Goal(450, John)], [Goal(451, John, time)])));
        Assert.Equal((450, 451), (change.OldPlayId, change.NewPlayId));
    }

    [Fact]
    public void NewIdAndTimeOutsideToleranceIsCalledBackPlusNew()
    {
        var changes = GoalMatcher.Compare([Goal(450, John)], [Goal(451, John, "05:30")]);

        Assert.Collection(changes,
            c => Assert.Equal(451, Assert.IsType<GoalAdded>(c).Goal.PlayId),
            c => Assert.Equal(450, Assert.IsType<GoalCalledBack>(c).Goal.PlayId));
    }

    [Fact]
    public void SameClockTimeInAnotherPeriodIsADifferentGoal()
    {
        var changes = GoalMatcher.Compare([Goal(450, John, period: 1)], [Goal(450, John, period: 1), Goal(700, John, period: 2)]);

        Assert.Equal(700, Assert.IsType<GoalAdded>(Assert.Single(changes)).Goal.PlayId);
    }

    [Fact]
    public void RenumberedAndReassignedReadsAsCalledBackPlusNew()
    {
        var changes = GoalMatcher.Compare([Goal(450, John)], [Goal(451, Jane)]);

        Assert.Collection(changes,
            c => Assert.Equal(Jane.Id, Assert.IsType<GoalAdded>(c).Goal.Scorer.Id),
            c => Assert.Equal(John.Id, Assert.IsType<GoalCalledBack>(c).Goal.Scorer.Id));
    }

    [Fact]
    public void UnreadableTimeFallsBackToMatchingById()
    {
        Assert.Empty(GoalMatcher.Compare([Goal(450, John, "")], [Goal(450, John, "")]));
        Assert.Collection(GoalMatcher.Compare([Goal(450, John, "")], [Goal(451, John, "")]),
            c => Assert.IsType<GoalAdded>(c),
            c => Assert.IsType<GoalCalledBack>(c));
    }

    [Fact]
    public void ChangesComeInApplyOrder()
    {
        // 1 renumbered (10→11), 2 reassigned, 3 new, 4 called back.
        var changes = GoalMatcher.Compare(
            [Goal(10, John, "01:00"), Goal(20, John, "02:00"), Goal(40, John, "04:00")],
            [Goal(30, Jane, "03:00"), Goal(20, Jane, "02:00"), Goal(11, John, "01:00")]);

        Assert.Collection(changes,
            c => Assert.IsType<GoalRenumbered>(c),
            c => Assert.IsType<GoalScorerChanged>(c),
            c => Assert.IsType<GoalAdded>(c),
            c => Assert.IsType<GoalCalledBack>(c));
    }
}
