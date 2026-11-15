namespace DraftPuck.DiscordBot;

/// <summary>
/// Where the web app's internal API is and its key, from the web app's <c>Application</c> section so both read the same
/// settings. The bot only talks to DraftPuck through this API.
/// </summary>
public class InternalApiOptions
{
    public const string SectionName = "Application";

    public string? BasePath { get; set; }
    public string? InternalApiBasePath { get; set; }
    public string? InternalApiKey { get; set; }

    public string? BaseAddress => InternalApiBasePath ?? BasePath;
}
