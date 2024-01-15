using Draftpuck.NhlApi.Models;
using System.Globalization;

namespace DraftPuck.Core.AutoMapper;

public static class MapperHelpers
{
    public static GameState MapGameState(string gameState)
    {
        return gameState is "FINAL" or "OFF" ? GameState.Final : gameState is "LIVE" or "CRIT" ? GameState.Live : GameState.Upcoming;
    }

    public static PeriodType MapPeriodType(string periodType)
    {
        return periodType == "OT" ? PeriodType.Overtime : periodType == "SO" ? PeriodType.Shootout : PeriodType.Regulation;
    }

    public static int MapMinutesRemaining(string timeRemaining)
    {
        if (!timeRemaining.Contains(':'))
        {
            return 0;
        }

        string[] split = timeRemaining.Split(':');
        return split.Length != 2 ? 0 : int.Parse(split[0]);
    }

    public static int MapSecondsRemaining(string timeRemaining)
    {
        if (!timeRemaining.Contains(':'))
        {
            return 0;
        }

        string[] split = timeRemaining.Split(':');
        return split.Length != 2 ? 0 : int.Parse(split[1]);
    }

    public static string MapLocation(NhlDefaultString placeName)
    {
        return placeName.Default.StartsWith("NY") ? "New York" : placeName.Default;
    }

    public static PlayType MapPlayType(string typeDescKey)
    {
        Dictionary<string, PlayType> dict = new()
        {
            { "period-start", PlayType.PeriodStart },
            { "faceoff", PlayType.Faceoff },
            { "delayed-penalty", PlayType.DelayedPenalty },
            { "penalty", PlayType.Penalty },
            { "shot-on-goal", PlayType.ShotOnGoal },
            { "blocked-shot", PlayType.BlockedShot },
            { "hit", PlayType.Hit },
            { "takeaway", PlayType.Takeaway },
            { "stoppage", PlayType.Stoppage },
            { "missed-shot", PlayType.MissedShot },
            { "giveaway", PlayType.Giveaway },
            { "goal", PlayType.Goal },
            { "period-end", PlayType.PeriodEnd },
            { "game-end", PlayType.GameEnd },
            { "challenge", PlayType.Challenge },
            { "shootout-complete", PlayType.ShootoutComplete }
        };

        return dict[typeDescKey];
    }

    public static int? MapPrimaryPlayerId(NhlPlay play)
    {
        PlayType playType = MapPlayType(play.TypeDescKey);
        if (playType == PlayType.Faceoff)
        {
            return play.Details.WinningPlayerId;
        }

        if (playType == PlayType.Penalty)
        {
            return play.Details.CommittedByPlayerId ?? play.Details.ServedByPlayerId;
        }

        return playType is PlayType.ShotOnGoal or PlayType.MissedShot
            ? play.Details.ShootingPlayerId
            : playType == PlayType.BlockedShot
            ? play.Details.BlockingPlayerId
            : playType == PlayType.Hit
            ? play.Details.HittingPlayerId
            : playType == PlayType.Goal ? play.Details.ScoringPlayerId : (play.Details?.PlayerId);
    }

    public static GameType MapGameType(int gameType)
    {
        return gameType == 1
            ? GameType.PreSeason
            : gameType == 2 ? GameType.RegularSeason : gameType == 3 ? GameType.Playoffs : GameType.Other;
    }

    public static int MapStrength(NhlSituation? situation, bool isHome)
    {
        return situation == null
            ? 5
            : isHome && situation.HomeTeam == null
            ? 5
            : !isHome && situation.AwayTeam == null
            ? 5
            : isHome
            ? situation.HomeTeam.Strength
            : situation.AwayTeam.Strength;
    }

    public static List<TeamSituation> MapSituations(NhlSituation? situation, bool isHome)
    {
        List<string>? teamSituations = isHome
            ? situation?.HomeTeam?.SituationDescriptions
            : situation?.AwayTeam?.SituationDescriptions;

        if (teamSituations == null)
        {
            return new();
        }

        List<TeamSituation> result = new();
        teamSituations.ForEach(ts =>
        {
            if (ts.Equals("PP"))
            {
                result.Add(TeamSituation.PowerPlay);
            }

            if (ts.Equals("PK"))
            {
                result.Add(TeamSituation.PenaltyKill);
            }

            if (ts.Equals("EN"))
            {
                result.Add(TeamSituation.EmptyNet);
            }
        });

        return result;
    }

    public static string KebabToCamelCase(string kebabString)
    {
        string strWithSpaces = kebabString.Replace('-', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(strWithSpaces);
    }
}
