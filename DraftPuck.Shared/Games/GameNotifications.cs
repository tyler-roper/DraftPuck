using DraftPuck.Shared.Users;
using MediatR;

namespace DraftPuck.Shared.Games;

public record GoalEventPayload(int GameId, PlayDto Play, PlayerDto Scorer);
public record GoalChangeEventPayload(int GameId, PlayDto Play, PlayerDto NewScorer, PlayerDto OldScorer);
public record GoalRemovedEventPayload(int GameId, int EventId, PlayerDto Scorer);
public record UpcomingPicksPayload(string LobbyCode, UserEntity User);

/// <summary>A poll produced a new copy of the game, already saved to the game cache.</summary>
public record GameUpdatedNotification(GameDto Game) : INotification;
public record GoalScoredNotification(GoalEventPayload Data) : INotification;
public record GoalChangedNotification(GoalChangeEventPayload Data) : INotification;
public record GoalRemovedNotification(GoalRemovedEventPayload Data) : INotification;
/// <summary>The NHL gave an unchanged goal a new play ID. Not a game event: nothing is announced.</summary>
public record GoalRenumberedNotification(int GameId, int OldEventId, int NewEventId) : INotification;
public record UpcomingPicksNotification(UpcomingPicksPayload Data) : INotification;

public record CheckUpcomingPicksRequest() : INotification;