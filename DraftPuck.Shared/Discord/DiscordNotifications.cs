using MediatR;

namespace DraftPuck.Shared.Discord;

public record DiscordServerJoinedNotification(string DiscordUserId) : INotification;