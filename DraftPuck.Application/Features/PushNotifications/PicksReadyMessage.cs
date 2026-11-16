using DraftPuck.Application.Features.Games;

namespace DraftPuck.Application.Features.PushNotifications;

/// <summary>The "time to pick" notification's text, naming the games whose picks just opened.</summary>
public static class PicksReadyMessage
{
    public const string Title = "Time to pick players!";

    /// <summary>Games named before the rest are counted, so the text fits a notification.</summary>
    public const int MaxNamedGames = 3;

    /// <summary>"Picks are open for EDM @ PHI, FLA @ DAL and CGY @ ANA.", in start order.</summary>
    public static string Body(IEnumerable<GameDto> games)
    {
        var matchups = games
            .DistinctBy(g => g.Id)
            .OrderBy(g => g.DateTime)
            .ThenBy(g => g.AwayTeam.Abbreviation)
            .Select(g => $"{g.AwayTeam.Abbreviation} @ {g.HomeTeam.Abbreviation}")
            .ToList();

        return $"Picks are open for {List(matchups)}.";
    }

    private static string List(List<string> items) => items.Count switch
    {
        0 => "your games",
        1 => items[0],
        <= MaxNamedGames => $"{string.Join(", ", items[..^1])} and {items[^1]}",
        _ => $"{string.Join(", ", items[..(MaxNamedGames - 1)])} and {items.Count - (MaxNamedGames - 1)} more"
    };
}
