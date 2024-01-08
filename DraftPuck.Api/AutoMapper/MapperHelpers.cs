using Draftpuck.Nhl.Models;

namespace DraftPuck.Api.AutoMapper
{
    public static class MapperHelpers
    {
        public static GameState MapGameState(string gameState)
        {
            if (gameState is "FINAL" or "OFF") return GameState.Final;
            if (gameState == "LIVE") return GameState.Live;
            return GameState.Upcoming;
        }

        public static PeriodType MapPeriodType(string periodType)
        {
            if (periodType == "OT") return PeriodType.Overtime;
            if (periodType == "SO") return PeriodType.Shootout;
            return PeriodType.Regulation;
        }

        public static int MapMinutesRemaining(string timeRemaining)
        {
            if (!timeRemaining.Contains(':')) return 0;
            var split = timeRemaining.Split(':');
            if (split.Length != 2) return 0;
            return int.Parse(split[0]);
        }

        public static int MapSecondsRemaining(string timeRemaining)
        {
            if (!timeRemaining.Contains(':')) return 0;
            var split = timeRemaining.Split(':');
            if (split.Length != 2) return 0;
            return int.Parse(split[1]);
        }

        public static string MapLocation(NhlDefaultString placeName)
        {
            if (placeName.Default.StartsWith("NY")) return "New York";
            else return placeName.Default;
        }

        public static PlayType MapPlayType(string typeDescKey)
        {
            var dict = new Dictionary<string, PlayType>()
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
                { "challenge", PlayType.Challenge }
            };

            return dict[typeDescKey];
        }

        public static int? MapPrimaryPlayerId(NhlPlay play)
        {
            var playType = MapPlayType(play.TypeDescKey);
            if (playType == PlayType.Faceoff) return play.Details.WinningPlayerId;
            if (playType == PlayType.Penalty) return play.Details.CommittedByPlayerId;
            if (playType is PlayType.ShotOnGoal or PlayType.MissedShot) return play.Details.ShootingPlayerId;
            if (playType == PlayType.BlockedShot) return play.Details.BlockingPlayerId;
            if (playType == PlayType.Hit) return play.Details.HittingPlayerId;
            if (playType == PlayType.Goal) return play.Details.ScoringPlayerId;
            return play.Details?.PlayerId;
        }

        public static GameType MapGameType(int gameType)
        {
            if (gameType == 1) return GameType.PreSeason;
            if (gameType == 2) return GameType.RegularSeason;
            if (gameType == 3) return GameType.Playoffs;
            return GameType.Other;
        }
    }
}
