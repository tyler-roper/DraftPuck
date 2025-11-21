namespace DraftPuck.Application.Features.Discord;

public record DiscordTokenResponseDto(string AccessToken, string RefreshToken, string TokenType);
public record DiscordIdentityResponseDto(string Id, string Username);