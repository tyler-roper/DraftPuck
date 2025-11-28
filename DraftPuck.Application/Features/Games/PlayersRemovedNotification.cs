namespace DraftPuck.Application.Features.Games;

public record PlayersRemovedNotification(Guid GameId, IReadOnlyList<int> PlayerIds) : INotification;
