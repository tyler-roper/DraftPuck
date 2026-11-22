namespace DraftPuck.Application.Features.Games;

/// <summary>
/// The NHL's play-by-play sometimes drops a goal, or its scorer, for a poll or two and then restores it, while the
/// score still counts the goal. Treating that as a called-back goal would revoke drinks and announce "goal removed" for
/// a goal that stands. So the score decides: a goal the feed stops listing is kept, as it was last seen, for as long as
/// its team's score still has room for it. Once the score drops, it's gone, and the goal matcher reports it called back.
/// </summary>
public static class GoalFeedGaps
{
    /// <summary>
    /// Puts back into <paramref name="game"/> the goals from <paramref name="previous"/> that the feed dropped while the
    /// score still counts them, and the scorers of goals that lost theirs. Returns the plays it restored.
    /// </summary>
    public static List<PlayDto> Restore(GameDto? previous, GameDto game)
    {
        var restored = new List<PlayDto>();
        // A shootout adds a goal to the winner's score with no goal play behind it, so the count can't be compared.
        if (previous == null || game.PeriodType == PeriodType.Shootout)
            return restored;

        var previousGoals = previous.Plays.Where(IsScoredGoal).DistinctBy(p => p.Id).ToDictionary(p => p.Id);

        // A goal still listed but without its scorer: keep the scorer we last saw. A real reassignment names someone.
        for (var i = 0; i < game.Plays.Count; i++)
        {
            var play = game.Plays[i];
            if (play.Type == PlayType.Goal && play.PrimaryPlayerId == null && previousGoals.TryGetValue(play.Id, out var before))
            {
                game.Plays[i] = before;
                restored.Add(before);
            }
        }

        // Goals no longer listed at all: keep them while their team's score is higher than the goals the feed lists.
        var listedIds = game.Plays.Select(p => p.Id).ToHashSet();
        foreach (var team in new[] { game.HomeTeam, game.AwayTeam })
        {
            var listed = game.Plays.Count(p => p.Type == PlayType.Goal && p.PrimaryTeamId == team.Id && p.PeriodType != PeriodType.Shootout);
            var missing = previousGoals.Values.Where(p => p.PrimaryTeamId == team.Id && !listedIds.Contains(p.Id));

            foreach (var goal in missing.Take(Math.Max(0, team.Score - listed)))
            {
                Insert(game.Plays, goal);
                restored.Add(goal);
            }
        }

        return restored;
    }

    private static bool IsScoredGoal(PlayDto play)
        => play.Type == PlayType.Goal && play.PrimaryPlayerId != null && play.PeriodType is PeriodType.Regulation or PeriodType.Overtime;

    /// <summary>Into its place in the game: before the first play that came after it.</summary>
    private static void Insert(List<PlayDto> plays, PlayDto goal)
    {
        var at = plays.FindIndex(p => p.Period > goal.Period
            || (p.Period == goal.Period && string.CompareOrdinal(p.TimeInPeriod, goal.TimeInPeriod) > 0));
        plays.Insert(at < 0 ? plays.Count : at, goal);
    }
}
