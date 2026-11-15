using MediatR;

namespace DraftPuck.Application.Features.Discord;

public record DiscordServerJoinedNotification(string DiscordUserId) : INotification;