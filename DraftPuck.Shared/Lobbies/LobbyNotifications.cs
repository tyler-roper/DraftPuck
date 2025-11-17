using DraftPuck.Shared.Games;
using MediatR;

namespace DraftPuck.Shared.Lobbies;

//Event Payloads
public record LobbyCreatedEventPayload(LobbyEntity Lobby);
public record LobbyEventPayload(LobbyEntity Lobby);
public record LobbyMemberEventPayload(LobbyEntity Lobby, LobbyMemberEntity Member);
public record LobbyNameChangeEventPayload(LobbyEntity Lobby, LobbyMemberEntity Member, string OldName);
public record LobbyPickEventPayload(LobbyEntity Lobby, LobbyMemberEntity Member, LobbyMemberPickEntity Pick);
public record LobbyDrinkEventPayload(LobbyEntity Lobby, LobbyMemberEntity Sender, LobbyMemberEntity Recipient, DrinkEntity Drink);
public record LobbyMessageEventPayload(LobbyEntity Lobby, LobbyMemberEntity Sender, MessageEntity Message);
public record DrinkInvalidatedPayload(LobbyEntity Lobby, LobbyMemberEntity Sender, LobbyMemberEntity Recipient, DrinkEntity Drink);
public record DrinkRemovedPayload(LobbyEntity Lobby, LobbyMemberEntity Member, DrinkEntity Drink);
public record DrinkAwardedPayload(LobbyEntity Lobby, LobbyMemberEntity Member, DrinkEntity Drink);
public record LobbyStateChangedPayload(string LobbyJoinCode);

//Notifications
public record LobbyCreatedNotification(LobbyCreatedEventPayload Data) : INotification;
public record UserJoinedLobbyNotification(LobbyMemberEventPayload Data) : INotification;
public record UserRejoinedLobbyNotification(LobbyMemberEventPayload Data) : INotification;
public record UserNameChangedNotification(LobbyNameChangeEventPayload Data) : INotification;
public record UserRemovedNotification(LobbyMemberEventPayload Data) : INotification;
public record UserLeftNotification(LobbyMemberEventPayload Data) : INotification;
public record UserPromotedNotification(LobbyMemberEventPayload Data) : INotification;
public record PickMadeNotification(LobbyPickEventPayload Data) : INotification;
public record PickRemovedNotification(LobbyPickEventPayload Data) : INotification;
public record DrinkAssignedNotification(LobbyDrinkEventPayload Data) : INotification;
public record MessageSentNotification(LobbyMessageEventPayload Data) : INotification;
public record LobbyEventCreatedNotification(LobbyEventEntity Event, string JoinCode) : INotification;
public record GlobalEventCreatedNotification(LobbyEventEntity Event) : INotification;
public record DrinkInvalidatedNotification(DrinkInvalidatedPayload Data) : INotification;
public record DrinkRemovedNotification(DrinkRemovedPayload Data) : INotification;
public record DrinkAwardedNotification(DrinkAwardedPayload Data) : INotification;
public record LobbyStateChangedNotification(LobbyStateChangedPayload Data) : INotification;
public record PicksReadyNotification(GameDto Game) : INotification;