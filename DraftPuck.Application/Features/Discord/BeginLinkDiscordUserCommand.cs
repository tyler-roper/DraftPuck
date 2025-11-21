namespace DraftPuck.Application.Features.Discord;

public record BeginLinkDiscordUserCommand(Guid DraftPuckUserId) : IRequest<string>;
