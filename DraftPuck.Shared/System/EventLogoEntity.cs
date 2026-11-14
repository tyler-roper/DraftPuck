namespace DraftPuck.Shared.System;

/// <summary>
/// A logo shown instead of the usual one during an event (a tournament, opening night...). Add a row to schedule one;
/// visits that start after it begins show it. When events overlap, the one that started latest wins.
/// </summary>
public class EventLogoEntity
{
    public Guid Id { get; set; }

    /// <summary>For people reading the table, e.g. "4 Nations Face-Off 2025".</summary>
    public string Name { get; set; } = null!;

    /// <summary>Shown from this moment (inclusive)...</summary>
    public DateTime StartsUtc { get; set; }

    /// <summary>...until this one (exclusive).</summary>
    public DateTime EndsUtc { get; set; }

    /// <summary>Replaces /img/logo.png (the splash and home pages). A site path like "/img/logo-4nations.png" or a full URL.</summary>
    public string LogoUrl { get; set; } = null!;

    /// <summary>Replaces /img/logo-wide.png (the lobby header). A site path or a full URL.</summary>
    public string WideLogoUrl { get; set; } = null!;
}
