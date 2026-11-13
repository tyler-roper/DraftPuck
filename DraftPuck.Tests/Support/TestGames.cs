namespace DraftPuck.Tests.Support;

/// <summary>Builds games shaped like the ones the NHL client and game cache return.</summary>
public static class TestGames
{
    public const int HomeTeamId = 1;
    public const int AwayTeamId = 2;

    /// <summary>Home player IDs are 100, 101, ...; away player IDs are 200, 201, .... Lower IDs have more goals.</summary>
    public static int HomePlayer(int rank) => 100 + rank;
    public static int AwayPlayer(int rank) => 200 + rank;

    /// <summary>
    /// A game whose rosters are complete (every player summary has a roster entry), with each roster sorted by goals,
    /// best first, the way ProcessGameCommandHandler leaves them.
    /// </summary>
    public static GameDto Create(int gameId, DateTime startUtc, GameState state = GameState.Upcoming, int playersPerTeam = 8)
    {
        var game = new GameDto
        {
            Id = gameId,
            DateTime = startUtc,
            GameState = state,
            GameType = GameType.RegularSeason,
            HomeTeam = new GameTeamDto { Id = HomeTeamId, Abbreviation = "HOM", Roster = Roster(HomeTeamId, HomePlayer, playersPerTeam) },
            AwayTeam = new GameTeamDto { Id = AwayTeamId, Abbreviation = "AWY", Roster = Roster(AwayTeamId, AwayPlayer, playersPerTeam) }
        };

        game.PlayerSummaries = [.. game.HomeTeam.Roster.Concat(game.AwayTeam.Roster).Select(p => new PlayerSummaryDto
        {
            Id = p.Id,
            TeamId = p.TeamId,
            FirstName = p.FirstName,
            LastName = p.LastName
        })];

        return game;
    }

    public static PlayDto Goal(int playId, int period, string timeInPeriod, int scorerId, int teamId) => new()
    {
        Id = playId,
        Period = period,
        PeriodType = PeriodType.Regulation,
        TimeInPeriod = timeInPeriod,
        TimeRemainingInPeriod = "10:00",
        Type = PlayType.Goal,
        PrimaryPlayerId = scorerId,
        PrimaryTeamId = teamId
    };

    private static List<PlayerDto> Roster(int teamId, Func<int, int> playerId, int count)
        => [.. Enumerable.Range(0, count).Select(rank => new PlayerDto
        {
            Id = playerId(rank),
            TeamId = teamId,
            FirstName = "Player",
            LastName = $"{teamId}-{rank}",
            Position = "C",
            Headshot = "",
            Goals = count - rank
        })];
}
