namespace DraftPuck.Shared.Discord;

public class DiscordOptions
{
    public const string SectionName = "Discord";

    public string? ClientId { get; set; }
    public string? RedirectUri { get; set; }
    public string? ClientSecret { get; set; }
    public string? BotToken { get; set; }
    public string? GuildId { get; set; }
}
