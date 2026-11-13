using DraftPuck.Infrastructure.Nhl.MappingProfiles;

namespace DraftPuck.Tests.Infrastructure;

public class NhlMappingHelpersTests
{
    [Theory]
    [InlineData("FUT", GameState.Upcoming)]
    [InlineData("PRE", GameState.Upcoming)] // warmups still count as before the game
    [InlineData("LIVE", GameState.Live)]
    [InlineData("CRIT", GameState.Live)]
    [InlineData("FINAL", GameState.Final)]
    [InlineData("OFF", GameState.Final)]
    public void MapsGameStates(string nhlState, GameState expected)
    {
        Assert.Equal(expected, NhlMappingHelpers.MapGameState(nhlState));
    }

    [Theory]
    [InlineData("REG", PeriodType.Regulation)]
    [InlineData("OT", PeriodType.Overtime)]
    [InlineData("SO", PeriodType.Shootout)]
    public void MapsPeriodTypes(string nhlType, PeriodType expected)
    {
        Assert.Equal(expected, NhlMappingHelpers.MapPeriodType(nhlType));
    }

    [Theory]
    [InlineData("12:34", 12, 34)]
    [InlineData("00:05", 0, 5)]
    [InlineData("", 0, 0)]
    [InlineData("END", 0, 0)]
    [InlineData("1:2:3", 0, 0)]
    public void ParsesTimeRemaining(string time, int minutes, int seconds)
    {
        Assert.Equal(minutes, NhlMappingHelpers.MapMinutesRemaining(time));
        Assert.Equal(seconds, NhlMappingHelpers.MapSecondsRemaining(time));
    }

    [Theory]
    [InlineData("goal", PlayType.Goal)]
    [InlineData("penalty", PlayType.Penalty)]
    [InlineData("game-end", PlayType.GameEnd)]
    [InlineData("something-new", PlayType.Unknown)]
    public void MapsPlayTypes(string nhlType, PlayType expected)
    {
        Assert.Equal(expected, NhlMappingHelpers.MapPlayType(nhlType));
    }
}
