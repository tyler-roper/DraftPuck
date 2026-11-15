using System.Net;

namespace DraftPuck.Application.Features.Lobbies.Events;

/// <summary>
/// The feed items for one run of bot picks for a game (bots pick for a game once its lineups are out): one for first
/// picks and one for replacements. "All bots" when every bot in the lobby made its first picks; otherwise the bots are
/// named (e.g. one added after the others had picked, or one replacing a scratched player).
/// </summary>
public static class BotPickEventTexts
{
    public const string Title = "Bot Picks";

    public static IEnumerable<(string Title, string Text)> Describe(BotPicksMadePayload data)
    {
        var matchup = $"{data.Game.HomeTeam.Abbreviation} vs {data.Game.AwayTeam.Abbreviation}";
        var firstPicks = data.Picks.Where(p => !p.IsReplacement).ToList();
        var replacements = data.Picks.Where(p => p.IsReplacement).ToList();

        if (firstPicks.Count > 1 && firstPicks.Count == data.BotCount)
            yield return (Title, $"All bots have made their picks for {matchup}.");
        else if (firstPicks.Count > 0)
            yield return (Title, $"{Names(firstPicks)} made {(firstPicks.Count == 1 ? "its" : "their")} picks for {matchup}.");

        if (replacements.Count > 0)
        {
            var what = replacements.Count == 1 && replacements[0].PickCount == 1 ? "a replacement" : "replacements";
            yield return (Title, $"{Names(replacements)} picked {what} for {matchup}.");
        }
    }

    /// <summary>"A", "A and B", "A, B and C", bold and HTML-encoded (the client renders event text as HTML).</summary>
    private static string Names(List<BotGamePicks> picks)
    {
        var names = picks.Select(p => $"<strong>{WebUtility.HtmlEncode(p.Bot.Name)}</strong>").ToList();
        return names.Count == 1 ? names[0] : $"{string.Join(", ", names[..^1])} and {names[^1]}";
    }
}
