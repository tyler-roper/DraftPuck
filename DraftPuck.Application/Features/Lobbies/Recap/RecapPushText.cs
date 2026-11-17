namespace DraftPuck.Application.Features.Lobbies.Recap;

/// <summary>The words of the day-after recap push.</summary>
public static class RecapPushText
{
    public const string Title = "🏆 Last night's recap is in";

    /// <summary>
    /// What the person won and earned, or else another member's award as a teaser, then where to look. For example
    /// "You won Best pick and Bully. You earned The Cup. See the awards and stats."
    /// </summary>
    public static string Body(IReadOnlyList<string> awardsWon, IReadOnlyList<string> achievementsEarned, LobbyAwardDto? teaser)
    {
        var parts = new List<string>();
        if (awardsWon.Count > 0) parts.Add($"You won {List(awardsWon)}.");
        if (achievementsEarned.Count > 0) parts.Add($"You earned {List(achievementsEarned)}.");
        if (parts.Count == 0 && teaser != null) parts.Add($"{teaser.MemberName} won {teaser.Title}.");
        parts.Add("See the awards and stats.");
        return string.Join(" ", parts);
    }

    /// <summary>"A", "A and B", "A, B and C".</summary>
    private static string List(IReadOnlyList<string> items) =>
        items.Count == 1 ? items[0] : $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}";
}
