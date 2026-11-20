using DraftPuck.Application.Common.Interfaces;
using DraftPuck.Application.Features.Games;
using MediatR;

namespace DraftPuck.Application.Features.Lobbies;

//Event Payloads
public record LobbyCreatedEventPayload(LobbyEntity Lobby, LobbyMemberEntity Host);
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
/// <summary>
/// One bot's picks for a game from a single run: its first picks (it had none for the game), or replacements for picks
/// it lost (a scratched player, or the host removing one).
/// </summary>
public record BotGamePicks(LobbyMemberEntity Bot, int PickCount, bool IsReplacement);
/// <summary>The bots that picked for a game in one run, and how many bots the lobby has.</summary>
public record BotPicksMadePayload(LobbyEntity Lobby, GameDto Game, IReadOnlyList<BotGamePicks> Picks, int BotCount);

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
/// <summary>The system removed a pick because its player left the game's lineup (e.g. scratched before puck drop).</summary>
public record PickScratchedNotification(LobbyPickEventPayload Data) : INotification;
public record DrinkAssignedNotification(LobbyDrinkEventPayload Data) : INotification;
public record MessageSentNotification(LobbyMessageEventPayload Data) : INotification;
public record LobbyEventCreatedNotification(LobbyEventEntity Event, string JoinCode) : INotification;
public record DrinkInvalidatedNotification(DrinkInvalidatedPayload Data) : INotification;
public record DrinkRemovedNotification(DrinkRemovedPayload Data) : INotification;
public record DrinkAwardedNotification(DrinkAwardedPayload Data) : INotification;
public record LobbyStateChangedNotification(LobbyStateChangedPayload Data) : INotification;
public record PicksReadyNotification(GameDto Game) : INotification;
/// <summary>These lobbies were just deactivated at the daily cutoff: their games are over.</summary>
public record LobbiesEndedNotification(IReadOnlyList<Guid> LobbyIds) : INotification;
public record BotPicksReadyNotification(GameDto Game) : INotification;
public record BotPicksMadeNotification(BotPicksMadePayload Data) : INotification;