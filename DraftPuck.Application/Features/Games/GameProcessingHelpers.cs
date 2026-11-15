namespace DraftPuck.Application.Features.Games;

public static class GameProcessingHelpers
{
    public static readonly int CutoffHours = 10;

    /// <summary>
    /// We cutover to the upcoming day's games at 10:00:00 UTC (roughly 5-6AM EST depending on DST).
    /// Whenever we need "current" games, we must apply this adjustment.
    /// </summary>
    public static DateTime AdjustDateTimeForCutoff(DateTime utcDate)
        => utcDate.AddHours(CutoffHours * -1);

    /// <summary>
    /// How long until a game is polled again, or null to stop: every 10 seconds while live, every minute in the 40 minutes
    /// before puck drop (and right after the morning schedule load), every 10 minutes before that, never once final.
    /// </summary>
    public static TimeSpan? GetPollDelay(GameDto game, DateTime nowUtc, bool isInitialPopulation)
    {
        if (isInitialPopulation) return TimeSpan.FromMinutes(1);

        return game.GameState switch
        {
            GameState.Live => TimeSpan.FromSeconds(10),
            GameState.Upcoming when game.DateTime - nowUtc <= TimeSpan.FromMinutes(40) => TimeSpan.FromMinutes(1),
            GameState.Upcoming => TimeSpan.FromMinutes(10),
            _ => null
        };
    }

    public static bool IsGameCurrent(GameDto game, DateTime nowUtc)
    {
        var nowAdjusted = AdjustDateTimeForCutoff(nowUtc).Date;
        var gameAdjusted = AdjustDateTimeForCutoff(game.DateTime).Date;
        return gameAdjusted == nowAdjusted;
    }

    /// <summary>
    /// The longest gap between polls that still counts as watching the game: live games are polled every 10 seconds,
    /// and a failed poll is retried after 2 minutes.
    /// </summary>
    public static readonly TimeSpan MaxWatchedGap = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Sets when each play happened (<see cref="PlayDto.DateTime"/>). A play from the previous poll keeps its time (by
    /// play ID, and across renumbered goals). A play that's new since then gets <paramref name="utcNow"/>, the real time
    /// of this poll, if we were watching: the previous poll was at most <see cref="MaxWatchedGap"/> ago. Otherwise it
    /// happened while we weren't (the Worker was down or stalled), so it gets an estimate from the game clock, kept
    /// between the previous poll and now. Without a previous poll at all (the first time we see the game, or the cached
    /// copy was lost), every play gets the estimate.
    /// </summary>
    /// <param name="utcNow">Real time, not test mode's simulated clock: it's compared with when picks were saved.</param>
    public static void SetPlayTimes(GameDto game, GameDto? previous, IEnumerable<GoalRenumbered> renumbers, DateTime utcNow)
    {
        if (previous == null)
        {
            foreach (var play in game.Plays)
                play.DateTime = Min(EstimatePlayTime(game, play), utcNow);
            return;
        }

        var previousById = previous.Plays.DistinctBy(p => p.Id).ToDictionary(p => p.Id);
        var renumberedFrom = renumbers.ToDictionary(r => r.NewPlayId, r => r.OldPlayId);
        var wasWatching = utcNow - previous.UpdatedUtc <= MaxWatchedGap;

        foreach (var play in game.Plays)
        {
            if (previousById.TryGetValue(play.Id, out var same))
                play.DateTime = same.DateTime;
            else if (renumberedFrom.TryGetValue(play.Id, out var oldId) && previousById.TryGetValue(oldId, out var old))
                play.DateTime = old.DateTime;
            else if (wasWatching)
                play.DateTime = utcNow;
            else
                play.DateTime = Max(previous.UpdatedUtc, Min(EstimatePlayTime(game, play), utcNow));
        }
    }

    /// <summary>
    /// When a play probably happened, from the scheduled start and the game clock: puck drop 10 minutes after the
    /// scheduled start, 40 minutes per period and 22 per intermission (shorter in regular-season overtime). Real games
    /// rarely keep that pace, so it can be well off; it's only for plays we didn't see happen, and test mode's replay.
    /// </summary>
    public static DateTime EstimatePlayTime(GameDto game, PlayDto play)
    {
        const int FULL_PERIOD_DURATION_MINS = 40;
        const int SHORT_PERIOD_DURATION_MINS = 7;
        const int FULL_INTERMISSION_MINS = 22;
        const int SHORT_INTERMISSION_MINS = 2;
        const int PRE_GAME_BUFFER_MINS = 10;

        var durationBeforePeriod = CalculateTimeBeforePeriod(play.Period, game.GameType,
                                                             FULL_PERIOD_DURATION_MINS,
                                                             SHORT_PERIOD_DURATION_MINS,
                                                             FULL_INTERMISSION_MINS,
                                                             SHORT_INTERMISSION_MINS);

        return game.DateTime.AddMinutes(PRE_GAME_BUFFER_MINS) + durationBeforePeriod + ParsePeriodTime(play.TimeInPeriod);
    }

    /// <summary>
    /// Test mode replays a past game on a simulated clock: only the plays whose estimated time has come are kept, and the
    /// game's state, clock and score are rebuilt from them.
    /// </summary>
    public static void HandleTestMode(GameDto game, ApplicationOptions appConfig)
    {
        if (!appConfig.IsTestMode) return;

        game.Plays = game.Plays.Where(play => EstimatePlayTime(game, play) <= appConfig.CurrentTimeUtc).ToList();

        var gameStarted = appConfig.CurrentTimeUtc >= game.DateTime;
        var gameEndedPlay = game.Plays.FirstOrDefault(play => play.Type == PlayType.GameEnd);

        if (!gameStarted) game.GameState = GameState.Upcoming;
        else if (gameEndedPlay == null) game.GameState = GameState.Live;
        else game.GameState = GameState.Final;

        var mostRecentPlay = game.Plays.LastOrDefault();

        if (game.GameState != GameState.Final && mostRecentPlay != null)
        {
            game.Period = mostRecentPlay.Period;
            game.PeriodType = mostRecentPlay.PeriodType;
            game.MinutesRemainingInPeriod = MapMinutesRemaining(mostRecentPlay.TimeRemainingInPeriod);
            game.SecondsRemainingInPeriod = MapSecondsRemaining(mostRecentPlay.TimeRemainingInPeriod);
        }
        else if (game.GameState == GameState.Upcoming)
        {
            game.Period = 1;
            game.PeriodType = PeriodType.Regulation;
            game.MinutesRemainingInPeriod = 20;
            game.SecondsRemainingInPeriod = 0;
        }

        game.HomeTeam.Score = game.Plays.Count(play => play.Type == PlayType.Goal && play.PrimaryTeamId == game.HomeTeam.Id);
        game.AwayTeam.Score = game.Plays.Count(play => play.Type == PlayType.Goal && play.PrimaryTeamId == game.AwayTeam.Id);

        var maxPeriod = game.Plays.Any() ? game.Plays.Max(p => p.Period) : 0;
        game.GoalsByPeriod = [.. Enumerable.Range(1, maxPeriod).Select(pNum => new PeriodSummaryDto()
        {
            AwayGoals = game.Plays.Count(play => play.Period == pNum && play.Type == PlayType.Goal && play.PrimaryTeamId == game.AwayTeam.Id),
            HomeGoals = game.Plays.Count(play => play.Period == pNum && play.Type == PlayType.Goal && play.PrimaryTeamId == game.HomeTeam.Id),
            Number = pNum,
            PeriodType = pNum <= 3 ? PeriodType.Regulation : game.GameType == GameType.Playoffs ? PeriodType.Overtime : pNum == 4 ? PeriodType.Overtime : PeriodType.Shootout
        })];
    }
    private static TimeSpan CalculateTimeBeforePeriod(int currentPeriod, GameType gameType,
        int fullPeriodMins, int shortPeriodMins,
        int fullIntermissionMins, int shortIntermissionMins)
    {
        var totalDuration = TimeSpan.Zero;
        var fullPeriodsCompleted = 0;
        var fullIntermissionsCompleted = 0;
        var shortPeriodsCompleted = 0;
        var shortIntermissionsCompleted = 0;

        if (gameType == GameType.Playoffs)
        {
            fullPeriodsCompleted = currentPeriod - 1;
            fullIntermissionsCompleted = fullPeriodsCompleted;
        }
        else
        {
            fullPeriodsCompleted = Math.Min(currentPeriod - 1, 3);
            shortPeriodsCompleted = Math.Max(currentPeriod - 4, 0);
            fullIntermissionsCompleted = Math.Min(fullPeriodsCompleted, 2);
            shortIntermissionsCompleted = Math.Max(currentPeriod - 4, 0);
        }

        totalDuration += TimeSpan.FromMinutes(fullPeriodsCompleted * fullPeriodMins);
        totalDuration += TimeSpan.FromMinutes(shortPeriodsCompleted * shortPeriodMins);
        totalDuration += TimeSpan.FromMinutes(fullIntermissionsCompleted * fullIntermissionMins);
        totalDuration += TimeSpan.FromMinutes(shortIntermissionsCompleted * shortIntermissionMins);

        return totalDuration;
    }

    private static TimeSpan ParsePeriodTime(string timeInPeriod)
    {
        if (string.IsNullOrEmpty(timeInPeriod) || !timeInPeriod.Contains(':'))
            return TimeSpan.Zero;

        var parts = timeInPeriod.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[0], out var minutes) && int.TryParse(parts[1], out var seconds))
        {
            return TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
        }

        return TimeSpan.Zero;
    }

    private static DateTime Min(DateTime dt1, DateTime dt2) => dt1 < dt2 ? dt1 : dt2;

    private static DateTime Max(DateTime dt1, DateTime dt2) => dt1 > dt2 ? dt1 : dt2;

    private static int MapMinutesRemaining(string timeRemaining)
    {
        if (string.IsNullOrEmpty(timeRemaining) || !timeRemaining.Contains(':')) return 0;
        var split = timeRemaining.Split(':');
        return split.Length != 2 || !int.TryParse(split[0], out var min) ? 0 : min;
    }

    private static int MapSecondsRemaining(string timeRemaining)
    {
        if (string.IsNullOrEmpty(timeRemaining) || !timeRemaining.Contains(':')) return 0;
        var split = timeRemaining.Split(':');
        return split.Length != 2 || !int.TryParse(split[1], out var sec) ? 0 : sec;
    }
}