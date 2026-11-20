using System.Text.Json;

namespace DraftPuck.Tests.Application.Games;

public class GameChangesDtoTests
{
    private static readonly DateTime LastPoll = new(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc);
    private static readonly int Scorer = TestGames.HomePlayer(2);

    /// <summary>A live game with two goals, as clients were last sent it, and the next poll's copy (equal, for now).</summary>
    private static (GameDto LastSent, GameDto Game) TwoPolls()
    {
        var lastSent = TestGames.Create(Seed.NewGameId(), LastPoll.AddHours(-1), GameState.Live);
        lastSent.UpdatedUtc = LastPoll;
        lastSent.Plays = [TestGames.Goal(1, 1, "05:00", Scorer, TestGames.HomeTeamId), TestGames.Goal(2, 1, "09:00", Scorer, TestGames.HomeTeamId)];

        var game = JsonSerializer.Deserialize<GameDto>(JsonSerializer.Serialize(lastSent))!;
        game.UpdatedUtc = LastPoll.AddSeconds(10);
        return (lastSent, game);
    }

    [Fact]
    public void AFirstPollSendsTheWholeGame()
    {
        var (_, game) = TwoPolls();

        var changes = GameChangesDto.Between(null, game);

        Assert.Null(changes.BaseUpdatedUtc);
        Assert.Equal([1, 2], changes.Plays.Select(p => p.Id));
        Assert.NotNull(changes.HomeTeam.Roster);
        Assert.NotNull(changes.AwayTeam.Roster);
        Assert.NotNull(changes.PlayerSummaries);
        Assert.NotNull(changes.FormerPlayers);
    }

    [Fact]
    public void WhatDidNotChangeIsLeftOut()
    {
        var (lastSent, game) = TwoPolls();

        var changes = GameChangesDto.Between(lastSent, game);

        Assert.Equal(LastPoll, changes.BaseUpdatedUtc);
        Assert.Equal(game.UpdatedUtc, changes.UpdatedUtc);
        Assert.Equal([1, 2], changes.PlayIds);
        Assert.Empty(changes.Plays);
        Assert.Null(changes.HomeTeam.Roster);
        Assert.Null(changes.AwayTeam.Roster);
        Assert.Null(changes.PlayerSummaries);
        Assert.Null(changes.FormerPlayers);
    }

    [Fact]
    public void TheClockAndScoreAreAlwaysSent()
    {
        var (lastSent, game) = TwoPolls();
        game.HomeTeam.Score = 3;
        game.MinutesRemainingInPeriod = 4;
        game.SecondsRemainingInPeriod = 5;

        var changes = GameChangesDto.Between(lastSent, game);

        Assert.Equal(3, changes.HomeTeam.Score);
        Assert.Equal("4:05", changes.TimeRemainingInPeriod);
        Assert.Equal(game.PickingOpensUtc, changes.PickingOpensUtc);
    }

    [Fact]
    public void NewAndChangedPlaysAreSentAndRemovedOnesDropOutOfTheOrder()
    {
        var (lastSent, game) = TwoPolls();
        game.Plays[0].PrimaryPlayerId = TestGames.HomePlayer(3); // scorer changed
        game.Plays.RemoveAt(1); // called back
        game.Plays.Add(TestGames.Goal(3, 2, "01:00", Scorer, TestGames.HomeTeamId));

        var changes = GameChangesDto.Between(lastSent, game);

        Assert.Equal([1, 3], changes.PlayIds);
        Assert.Equal([1, 3], changes.Plays.Select(p => p.Id));
    }

    [Fact]
    public void OnlyAChangedRosterIsSent()
    {
        var (lastSent, game) = TwoPolls();
        game.AwayTeam.Roster[0].Goals++;

        var changes = GameChangesDto.Between(lastSent, game);

        Assert.Null(changes.HomeTeam.Roster);
        Assert.Same(game.AwayTeam.Roster, changes.AwayTeam.Roster);
    }

    [Fact]
    public void ChangedLineupsAreSent()
    {
        var (lastSent, game) = TwoPolls();
        var scratched = game.PlayerSummaries[^1];
        game.PlayerSummaries.Remove(scratched);
        game.FormerPlayers = [scratched];

        var changes = GameChangesDto.Between(lastSent, game);

        Assert.Same(game.PlayerSummaries, changes.PlayerSummaries);
        Assert.Same(game.FormerPlayers, changes.FormerPlayers);
    }
}
