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
    public void IsPickWindowOpen_OpensThirtyMinutesBeforeStart(int minutesFromStart, bool expected)
    {
        var game = TestGames.Create(1, Start);

        Assert.Equal(expected, GameProcessingHelpers.IsPickWindowOpen(game, Start.AddMinutes(minutesFromStart)));
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
    public void SetPlayDateTimes_EstimatesWallClockTime(GameType gameType, int period, string timeInPeriod, double expectedMinutesAfterStart)
    {
        var game = TestGames.Create(1, Start);
        game.GameType = gameType;
        game.Plays = [TestGames.Goal(1, period, timeInPeriod, TestGames.HomePlayer(0), TestGames.HomeTeamId)];

        GameProcessingHelpers.SetPlayDateTimes(game, currentUtcTime: Start.AddDays(1), isTestMode: false);

        var expected = Start.AddMinutes(expectedMinutesAfterStart);
        Assert.Equal(expected, game.Plays[0].DateTime, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void SetPlayDateTimes_NeverPlacesPlaysInTheFuture()
    {
        var game = TestGames.Create(1, Start);
        game.Plays = [TestGames.Goal(1, 3, "15:00", TestGames.HomePlayer(0), TestGames.HomeTeamId)];
        var now = Start.AddMinutes(30); // the estimate would be hours from now

        GameProcessingHelpers.SetPlayDateTimes(game, now, isTestMode: false);

        Assert.Equal(now, game.Plays[0].DateTime);
    }

    [Fact]
    public void SetPlayDateTimes_KeepsTimesAlreadyAssignedToCachedPlays()
    {
        var cachedTime = Start.AddMinutes(12);
        var cached = TestGames.Create(1, Start);
        cached.Plays = [TestGames.Goal(1, 2, "10:00", TestGames.HomePlayer(0), TestGames.HomeTeamId)];
        cached.Plays[0].DateTime = cachedTime;

        var updated = TestGames.Create(1, Start);
        updated.Plays = [TestGames.Goal(1, 2, "10:00", TestGames.HomePlayer(0), TestGames.HomeTeamId)];

        GameProcessingHelpers.SetPlayDateTimes(updated, Start.AddDays(1), isTestMode: false, cached);

        Assert.Equal(cachedTime, updated.Plays[0].DateTime);
    }
}
