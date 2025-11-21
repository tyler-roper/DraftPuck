using DraftPuck.Shared.Users;
using MediatR;

namespace DraftPuck.Shared.Games;

public record GoalEventPayload(int GameId, PlayDto Play, PlayerDto Scorer);
public record GoalChangeEventPayload(int GameId, PlayDto Play, PlayerDto NewScorer, PlayerDto OldScorer);
public record GoalRemovedEventPayload(int GameId, int EventId, PlayerDto Scorer);
public record UpcomingPicksPayload(string LobbyCode, UserEntity User);

public record GoalScoredNotification(GoalEventPayload Data) : INotification;
public record GoalChangedNotification(GoalChangeEventPayload Data) : INotification;
public record GoalRemovedNotification(GoalRemovedEventPayload Data) : INotification;
public record UpcomingPicksNotification(UpcomingPicksPayload Data) : INotification;

public record CheckUpcomingPicksRequest() : INotification;