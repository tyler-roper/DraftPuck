using DraftPuck.Application.Features.Games;

namespace DraftPuck.Tests.Application.Games;

public class GameProcessingHelpersTests
{
    private static readonly DateTime Start = new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc); // 7 PM ET on Nov 12

    [Theory]
    [InlineData(-31, false)]
    [InlineData(-30, true)]
    [InlineData(-1, true)]
    [InlineData(90, true)] // picking stays open once the game has started
    public void PickingOpensThirtyMinutesBeforeStart(int minutesFromStart, bool expected)
    {
        var game = TestGames.Create(1, Start, minutesFromStart > 0 ? GameState.Live : GameState.Upcoming);

        Assert.Equal(Start.AddMinutes(-30), game.PickingOpensUtc);
        Assert.Equal(expected, game.IsPickingOpen(Start.AddMinutes(minutesFromStart)));
    }

    [Fact]
    public void PickingClosesWhenTheGameIsFinal()
    {
        var game = TestGames.Create(1, Start, GameState.Final);

        Assert.True(game.IsPickingClosed);
        Assert.False(game.IsPickingOpen(Start.AddHours(3)));
    }

    [Theory]
    [InlineData(GameState.Upcoming, true)]
    [InlineData(GameState.Live, false)]
    [InlineData(GameState.Final, false)]
    public void MembersCanRemoveTheirPicksOnlyBeforePuckDrop(GameState state, bool expected)
        => Assert.Equal(expected, TestGames.Create(1, Start, state).CanMembersRemovePicks);

    [Fact]
    public void PickRulesReachClientsAsPartOfTheGame()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(TestGames.Create(1, Start, GameState.Final));

        Assert.Contains("\"PickingOpensUtc\"", json);
        Assert.Contains("\"IsPickingClosed\":true", json);
        Assert.Contains("\"CanMembersRemovePicks\":false", json);
    }

    [Theory]
    [InlineData("2026-11-12T15:00:00Z", true)]  // earlier the same NHL day
    [InlineData("2026-11-13T09:59:59Z", true)]  // late night, before the 10:00 UTC cutoff
    [InlineData("2026-11-13T10:00:00Z", false)] // the cutoff moves to the next day's games
    [InlineData("2026-11-12T09:59:59Z", false)] // still the previous NHL day
    public void IsGameCurrent_UsesTenAmUtcCutoff(string nowUtc, bool expected)
    {
        var game = TestGames.Create(1, Start);
        var now = DateTime.Parse(nowUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal);

        Assert.Equal(expected, GameProcessingHelpers.IsGameCurrent(game, now));
    }

    [Theory]
    // Estimated play time = start + 10 min pregame + earlier periods (40 min) and intermissions (22 min) + time in period.
    [InlineData(GameType.RegularSeason, 1, "05:00", 15)]
    [InlineData(GameType.RegularSeason, 2, "00:00", 10 + 40 + 22)]
    [InlineData(GameType.RegularSeason, 3, "19:59", 10 + 80 + 44 + 19.983333333333334)]
    // Regular-season overtime starts right after the third period, with no intermission counted.
    [InlineData(GameType.RegularSeason, 4, "01:30", 10 + 120 + 44 + 1.5)]
    // Playoff overtime periods are full periods, each after a full intermission.
    [InlineData(GameType.Playoffs, 4, "00:00", 10 + 120 + 66)]
    [InlineData(GameType.Playoffs, 5, "00:00", 10 + 160 + 88)]
    public void EstimatePlayTime_FromTheScheduledStartAndTheGameClock(GameType gameType, int period, string timeInPeriod, double expectedMinutesAfterStart)
    {
        var game = TestGames.Create(1, Start);
        game.GameType = gameType;
        var play = TestGames.Goal(1, period, timeInPeriod, TestGames.HomePlayer(0), TestGames.HomeTeamId);

        Assert.Equal(Start.AddMinutes(expectedMinutesAfterStart), GameProcessingHelpers.EstimatePlayTime(game, play), TimeSpan.FromMilliseconds(1));
    }

    private static PlayDto GoalPlay(int id, DateTime? dateTime = null, int period = 1, string timeInPeriod = "05:00")
    {
        var play = TestGames.Goal(id, period, timeInPeriod, TestGames.HomePlayer(0), TestGames.HomeTeamId);
        if (dateTime != null)
            play.DateTime = dateTime.Value;
        return play;
    }

    [Fact]
    public void SetPlayTimes_StampsPlaysSeenHappeningWithThePollTimeAndKeepsEarlierOnes()
    {
        var seen = Start.AddMinutes(5);
        var previous = TestGames.Create(1, Start);
        previous.Plays = [GoalPlay(1, seen)];
        var game = TestGames.Create(1, Start);
        // A goal at 01:00 of the first period, which the estimate would put 4 minutes before the earlier one.
        game.Plays = [GoalPlay(1), GoalPlay(2, timeInPeriod: "01:00")];
        var now = Start.AddMinutes(50);
        previous.UpdatedUtc = now.AddSeconds(-10); // polled 10 seconds ago, as while live

        GameProcessingHelpers.SetPlayTimes(game, previous, [], now);

        Assert.Equal(seen, game.Plays[0].DateTime); // kept
        Assert.Equal(now, game.Plays[1].DateTime);  // new this poll: when we saw it, not the estimate
    }

    // E.g. the Worker stalled or was down for half an hour mid-game: the previous copy is old, so plays new since then
    // weren't seen happening. They get the estimate, kept within the gap.
    [Fact]
    public void SetPlayTimes_EstimatesPlaysThatHappenedWhileWeWerentPolling()
    {
        var previous = TestGames.Create(1, Start);
        previous.UpdatedUtc = Start.AddMinutes(62);
        var game = TestGames.Create(1, Start);
        game.Plays =
        [
            GoalPlay(1, period: 2, timeInPeriod: "00:00"),  // estimated at +72: inside the gap
            GoalPlay(2, period: 1, timeInPeriod: "19:50")   // estimated at +29.8: before the gap, so it can't be right
        ];
        var now = Start.AddMinutes(95);

        GameProcessingHelpers.SetPlayTimes(game, previous, [], now);

        Assert.Equal(Start.AddMinutes(72), game.Plays[0].DateTime);
        Assert.Equal(previous.UpdatedUtc, game.Plays[1].DateTime);
    }

    [Fact]
    public void SetPlayTimes_KeepsTheTimeWhenAGoalIsRenumbered()
    {
        var seen = Start.AddMinutes(5);
        var previous = TestGames.Create(1, Start);
        previous.Plays = [GoalPlay(450, seen)];
        var game = TestGames.Create(1, Start);
        game.Plays = [GoalPlay(451)];

        GameProcessingHelpers.SetPlayTimes(game, previous, [new GoalRenumbered(450, 451)], Start.AddMinutes(20));

        Assert.Equal(seen, game.Plays[0].DateTime);
    }

    [Fact]
    public void SetPlayTimes_EstimatesPlaysThatHappenedBeforeWeWereWatching()
    {
        var game = TestGames.Create(1, Start);
        game.Plays = [GoalPlay(1, period: 1, timeInPeriod: "05:00")];

        GameProcessingHelpers.SetPlayTimes(game, previous: null, [], Start.AddHours(2));

        Assert.Equal(Start.AddMinutes(15), game.Plays[0].DateTime);
    }

    [Fact]
    public void SetPlayTimes_NeverEstimatesAPlayIntoTheFuture()
    {
        var game = TestGames.Create(1, Start);
        game.Plays = [GoalPlay(1, period: 3, timeInPeriod: "15:00")];
        var now = Start.AddMinutes(30); // the estimate would be hours from now

        GameProcessingHelpers.SetPlayTimes(game, previous: null, [], now);

        Assert.Equal(now, game.Plays[0].DateTime);
    }
}
