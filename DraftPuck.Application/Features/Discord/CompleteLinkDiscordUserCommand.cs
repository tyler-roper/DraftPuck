namespace DraftPuck.Application.Features.Discord;

public record CompleteLinkDiscordUserCommand(string Code, string State) : IRequest<bool>;
