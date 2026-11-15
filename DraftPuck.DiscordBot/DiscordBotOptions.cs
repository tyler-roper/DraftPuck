namespace DraftPuck.DiscordBot;

/// <summary>The bot's own Discord settings, from the same <c>Discord</c> section the web app reads its OAuth settings from.</summary>
public class DiscordBotOptions
{
    public const string SectionName = "Discord";

    public string? BotToken { get; set; }
    public string? GuildId { get; set; }
}
