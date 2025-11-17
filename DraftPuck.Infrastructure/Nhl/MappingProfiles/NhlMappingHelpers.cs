using DraftPuck.Infrastructure.Nhl.Models;

namespace DraftPuck.Infrastructure.Nhl.MappingProfiles;

public static class NhlMappingHelpers
{
    public static GameState MapGameState(string gameState) => gameState switch
    {
        "FINAL" or "OFF" => GameState.Final,
        "LIVE" or "CRIT" => GameState.Live,
        _ => GameState.Upcoming
    };

    public static PeriodType MapPeriodType(string periodType) => periodType switch
    {
        "OT" => PeriodType.Overtime,
        "SO" => PeriodType.Shootout,
        _ => PeriodType.Regulation
    };

    public static int MapMinutesRemaining(string timeRemaining)
    {
        if (string.IsNullOrEmpty(timeRemaining) || !timeRemaining.Contains(':')) return 0;
        var split = timeRemaining.Split(':');
        return split.Length != 2 || !int.TryParse(split[0], out var min) ? 0 : min;
    }

    public static int MapSecondsRemaining(string timeRemaining)
    {
        if (string.IsNullOrEmpty(timeRemaining) || !timeRemaining.Contains(':')) return 0;
        var split = timeRemaining.Split(':');
        return split.Length != 2 || !int.TryParse(split[1], out var sec) ? 0 : sec;
    }

    public static string MapLocation(NhlDefaultString placeName)
    {
        return placeName?.Default?.StartsWith("NY") == true ? "New York" : placeName?.Default ?? string.Empty;
    }

    public static PlayType MapPlayType(string typeDescKey) => typeDescKey switch
    {
        "period-start" => PlayType.PeriodStart,
        "faceoff" => PlayType.Faceoff,
        "delayed-penalty" => PlayType.DelayedPenalty,
        "penalty" => PlayType.Penalty,
        "shot-on-goal" => PlayType.ShotOnGoal,
        "blocked-shot" => PlayType.BlockedShot,
        "hit" => PlayType.Hit,
        "takeaway" => PlayType.Takeaway,
        "stoppage" => PlayType.Stoppage,
        "missed-shot" => PlayType.MissedShot,
        "giveaway" => PlayType.Giveaway,
        "goal" => PlayType.Goal,
        "period-end" => PlayType.PeriodEnd,
        "game-end" => PlayType.GameEnd,
        "challenge" => PlayType.Challenge,
        "shootout-complete" => PlayType.ShootoutComplete,
        _ => PlayType.Unknown
    };

    public static int? MapPrimaryPlayerId(NhlPlay play)
    {
        var playType = MapPlayType(play.TypeDescKey);
        return playType switch
        {
            PlayType.Faceoff => play.Details.WinningPlayerId,
            PlayType.Penalty => play.Details.CommittedByPlayerId ?? play.Details.ServedByPlayerId,
            PlayType.ShotOnGoal or PlayType.MissedShot => play.Details.ShootingPlayerId,
            PlayType.BlockedShot => play.Details.BlockingPlayerId,
            PlayType.Hit => play.Details.HittingPlayerId,
            PlayType.Goal => play.Details.ScoringPlayerId,
            _ => play.Details?.PlayerId
        };
    }

    public static GameType MapGameType(int gameType) => gameType switch
    {
        1 => GameType.PreSeason,
        2 => GameType.RegularSeason,
        3 => GameType.Playoffs,
        _ => GameType.Other
    };

    public static int MapStrength(NhlSituation? situation, bool isHome)
    {
        var teamSituation = isHome ? situation?.HomeTeam : situation?.AwayTeam;
        return teamSituation?.Strength ?? 5;
    }

    public static List<TeamSituation> MapSituations(NhlSituation? situation, bool isHome)
    {
        var teamSituations = isHome
            ? situation?.HomeTeam?.SituationDescriptions
            : situation?.AwayTeam?.SituationDescriptions;

        if (teamSituations == null)
        {
            return [];
        }

        return teamSituations
            .Select(ts => ts switch
            {
                "PP" => TeamSituation.PowerPlay,
                "PK" => TeamSituation.PenaltyKill,
                "EN" => TeamSituation.EmptyNet,
                _ => (TeamSituation?)null
            })
            .Where(ts => ts.HasValue)
            .Select(ts => ts!.Value)
            .ToList();
    }

    public static string KebabToCamelCase(string kebabString)
    {
        if (string.IsNullOrEmpty(kebabString))
            return string.Empty;

        var parts = kebabString.Split('-');
        return parts[0] + string.Concat(parts.Skip(1).Select(p =>
            p.Length > 0 ? char.ToUpperInvariant(p[0]) + p.Substring(1) : string.Empty
        ));
    }
}