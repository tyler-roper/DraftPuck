using System.Text.Json;

namespace DraftPuck.Application.Features.Games;

/// <summary>
/// What a poll changed in a game, pushed to clients instead of the whole game. A live game is ~50 KB, mostly plays
/// that never change once they've happened, and every game is pushed to every lobby page every 10 seconds.
/// <para>
/// The small fields are always sent. Plays: the full ordered list of IDs, with only the plays that are new or changed.
/// Rosters and player lists: only when they changed (null means "as you have it").
/// </para>
/// <para>
/// Changes apply only to the copy they were made from, <see cref="BaseUpdatedUtc"/>; a client holding any other copy
/// (it missed a push) fetches the game instead. Null means this is the whole game (no previous poll to compare with).
/// </para>
/// </summary>
public class GameChangesDto
{
    public int Id { get; set; }
    public DateTime? BaseUpdatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime DateTime { get; set; }
    public GameType GameType { get; set; }
    public GameState GameState { get; set; }
    public int Period { get; set; }
    public PeriodType PeriodType { get; set; }
    public int MinutesRemainingInPeriod { get; set; }
    public int SecondsRemainingInPeriod { get; set; }
    public string TimeRemainingInPeriod { get; set; } = null!;
    public List<PeriodSummaryDto> GoalsByPeriod { get; set; } = [];
    public DateTime PickingOpensUtc { get; set; }
    public bool IsPickingClosed { get; set; }
    public bool CanMembersRemovePicks { get; set; }
    public GameTeamChangesDto HomeTeam { get; set; } = new();
    public GameTeamChangesDto AwayTeam { get; set; } = new();

    /// <summary>Every play's ID, in the game's order.</summary>
    public List<int> PlayIds { get; set; } = [];

    /// <summary>The plays that are new or changed. Every other ID in <see cref="PlayIds"/> is a play the client holds.</summary>
    public List<PlayDto> Plays { get; set; } = [];

    public List<PlayerSummaryDto>? PlayerSummaries { get; set; }
    public List<PlayerSummaryDto>? FormerPlayers { get; set; }

    /// <summary>What changed from <paramref name="lastSent"/> (the copy clients were last sent) to <paramref name="game"/>.</summary>
    public static GameChangesDto Between(GameDto? lastSent, GameDto game)
    {
        var sentPlays = lastSent?.Plays.DistinctBy(p => p.Id).ToDictionary(p => p.Id, Json) ?? [];

        return new GameChangesDto
        {
            Id = game.Id,
            BaseUpdatedUtc = lastSent?.UpdatedUtc,
            UpdatedUtc = game.UpdatedUtc,
            DateTime = game.DateTime,
            GameType = game.GameType,
            GameState = game.GameState,
            Period = game.Period,
            PeriodType = game.PeriodType,
            MinutesRemainingInPeriod = game.MinutesRemainingInPeriod,
            SecondsRemainingInPeriod = game.SecondsRemainingInPeriod,
            TimeRemainingInPeriod = game.TimeRemainingInPeriod,
            GoalsByPeriod = game.GoalsByPeriod,
            PickingOpensUtc = game.PickingOpensUtc,
            IsPickingClosed = game.IsPickingClosed,
            CanMembersRemovePicks = game.CanMembersRemovePicks,
            HomeTeam = GameTeamChangesDto.Between(lastSent?.HomeTeam, game.HomeTeam),
            AwayTeam = GameTeamChangesDto.Between(lastSent?.AwayTeam, game.AwayTeam),
            PlayIds = [.. game.Plays.Select(p => p.Id)],
            Plays = [.. game.Plays.Where(p => !sentPlays.TryGetValue(p.Id, out var sent) || sent != Json(p))],
            PlayerSummaries = IfChanged(lastSent?.PlayerSummaries, game.PlayerSummaries),
            FormerPlayers = IfChanged(lastSent?.FormerPlayers, game.FormerPlayers),
        };
    }

    internal static List<T>? IfChanged<T>(List<T>? sent, List<T> current) => sent != null && Json(sent) == Json(current) ? null : current;

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}

/// <summary>A team's part of <see cref="GameChangesDto"/>. Name and logos never change during a game, so aren't sent.</summary>
public class GameTeamChangesDto
{
    public int Score { get; set; }
    public List<TeamSituation> Situations { get; set; } = [];
    public int Strength { get; set; }

    /// <summary>Null when unchanged.</summary>
    public List<PlayerDto>? Roster { get; set; }

    public static GameTeamChangesDto Between(GameTeamDto? lastSent, GameTeamDto team) => new()
    {
        Score = team.Score,
        Situations = team.Situations,
        Strength = team.Strength,
        Roster = GameChangesDto.IfChanged(lastSent?.Roster, team.Roster),
    };
}
