namespace DraftPuck.DiscordBot;

/// <summary>The bot's own Discord settings, from the same <c>Discord</c> section the web app reads its OAuth settings from.</summary>
public class DiscordBotOptions
{
    public const string SectionName = "Discord";

    public string? BotToken { get; set; }
    public string? GuildId { get; set; }
}

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
