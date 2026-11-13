namespace DraftPuck.Application.Features.Games;

/// <summary>A goal that counts for drinks (regulation or overtime, scorer on a roster), as seen in one poll.</summary>
public record TrackedGoal(PlayDto Play, PlayerDto Scorer)
{
    public int PlayId => Play.Id;

    /// <summary>
    /// The goals in a game that count: regulation and overtime (not shootout), credited to a player on one of the rosters.
    /// Nothing counts until a roster has been loaded.
    /// </summary>
    public static List<TrackedGoal> CountedIn(GameDto game)
    {
        var players = game.HomeTeam.Roster.Concat(game.AwayTeam.Roster).ToList();
        if (players.Count == 0) return [];

        return [.. game.Plays
            .Where(play => play.Type == PlayType.Goal
                && play.PrimaryPlayerId != null
                && play.PeriodType is PeriodType.Regulation or PeriodType.Overtime)
            .DistinctBy(play => play.Id)
            .Select(play => (Play: play, Scorer: players.FirstOrDefault(p => p.Id == play.PrimaryPlayerId)))
            .Where(x => x.Scorer != null)
            .Select(x => new TrackedGoal(x.Play, x.Scorer!))];
    }

    /// <summary>Seconds into the period, or null when the NHL sent a time we can't read.</summary>
    public int? SecondsIntoPeriod => ParseSeconds(Play.TimeInPeriod);

    private static int? ParseSeconds(string? time)
    {
        var parts = time?.Split(':');
        return parts is [var m, var s] && int.TryParse(m, out var minutes) && int.TryParse(s, out var seconds)
            ? minutes * 60 + seconds
            : null;
    }
}

public abstract record GoalChange;
/// <summary>A goal not seen before.</summary>
public record GoalAdded(TrackedGoal Goal) : GoalChange;
/// <summary>The same goal (same NHL play ID) credited to a different scorer.</summary>
public record GoalScorerChanged(TrackedGoal Before, TrackedGoal After) : GoalChange;
/// <summary>A goal that no longer appears.</summary>
public record GoalCalledBack(TrackedGoal Goal) : GoalChange;
/// <summary>The NHL gave an unchanged goal a new play ID. Nothing happened in the game; stored references need updating.</summary>
public record GoalRenumbered(int OldPlayId, int NewPlayId) : GoalChange;

/// <summary>
/// Works out what happened to the goals between two polls. The NHL doesn't keep play IDs stable (450 can become 451),
/// so goals are matched by content first and by ID second:
/// <list type="number">
/// <item>Same scorer, same period, clock within <see cref="TimeToleranceSeconds"/>: the same goal, whatever the ID.</item>
/// <item>Otherwise, same play ID: a different scorer is a scorer change; the same scorer means only the time was
/// corrected, which is not a change.</item>
/// <item>Anything left over is a new goal (now) or a called-back goal (before).</item>
/// </list>
/// A goal that is renumbered and reassigned in the same poll can't be tied to its old self, so it reads as called back plus new.
/// </summary>
public static class GoalMatcher
{
    public const int TimeToleranceSeconds = 3;

    /// <summary>Changes in the order they should be applied: renumbers, scorer changes, new goals, then called-back goals.</summary>
    public static List<GoalChange> Compare(IReadOnlyCollection<TrackedGoal> before, IReadOnlyCollection<TrackedGoal> after)
    {
        var unmatchedBefore = before.ToList();
        var unmatchedAfter = new List<TrackedGoal>();
        var renumbered = new List<GoalChange>();
        var scorerChanged = new List<GoalChange>();

        foreach (var goal in after)
        {
            var same = unmatchedBefore
                .Where(b => IsSameContent(b, goal))
                .OrderBy(b => b.PlayId == goal.PlayId ? 0 : 1)
                .FirstOrDefault();

            if (same == null)
            {
                unmatchedAfter.Add(goal);
                continue;
            }

            unmatchedBefore.Remove(same);
            if (same.PlayId != goal.PlayId)
                renumbered.Add(new GoalRenumbered(same.PlayId, goal.PlayId));
        }

        var added = new List<GoalChange>();
        foreach (var goal in unmatchedAfter)
        {
            var sameId = unmatchedBefore.FirstOrDefault(b => b.PlayId == goal.PlayId);
            if (sameId == null)
            {
                added.Add(new GoalAdded(goal));
                continue;
            }

            unmatchedBefore.Remove(sameId);
            if (sameId.Scorer.Id != goal.Scorer.Id)
                scorerChanged.Add(new GoalScorerChanged(sameId, goal));
        }

        return [.. renumbered, .. scorerChanged, .. added, .. unmatchedBefore.Select(g => new GoalCalledBack(g))];
    }

    private static bool IsSameContent(TrackedGoal a, TrackedGoal b)
        => a.Scorer.Id == b.Scorer.Id
            && a.Play.Period == b.Play.Period
            && a.SecondsIntoPeriod is { } secondsA
            && b.SecondsIntoPeriod is { } secondsB
            && Math.Abs(secondsA - secondsB) <= TimeToleranceSeconds;
}
