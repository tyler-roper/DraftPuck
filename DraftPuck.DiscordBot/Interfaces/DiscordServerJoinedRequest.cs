namespace DraftPuck.DiscordBot.Interfaces;

/// <summary>The body of <c>POST api/internal/discord-server-joined</c>, as the web app's DiscordServerJoinedRequestDto expects it.</summary>
public record DiscordServerJoinedRequest(string DiscordUserId);
