using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.RegularExpressions;

namespace DraftPuck.Application.Features.Lobbies.Drinks;

/// <summary>The optional message a member adds when handing out a drink ("That's for the Oilers goal.").</summary>
public static partial class DrinkMessages
{
    public const int MaxLength = 80;

    /// <summary>
    /// The message as stored: whitespace (including line breaks) collapsed and trimmed, or null when there's nothing to
    /// say. Throws if it's longer than <see cref="MaxLength"/>.
    /// </summary>
    public static string? Normalize(string? message)
    {
        if (message == null) return null;

        var normalized = Whitespace().Replace(message, " ").Trim();
        if (normalized.Length == 0) return null;
        if (normalized.Length > MaxLength)
            throw new ValidationException($"A drink message must be {MaxLength} characters or fewer.");

        return normalized;
    }

    /// <summary>
    /// The message as a quote for event text, which the client renders as HTML and fills tokens in ({{name}}): encoded,
    /// with braces as entities so a message can't become a token.
    /// </summary>
    public static string Quote(string message)
    {
        var encoded = WebUtility.HtmlEncode(message).Replace("{", "&#123;").Replace("}", "&#125;");
        return $"<span class=\"d-block fst-italic\">“{encoded}”</span>";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
