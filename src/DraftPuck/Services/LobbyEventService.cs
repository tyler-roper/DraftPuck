using DraftPuck.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DraftPuck.Services
{
    public class LobbyEventService : ILobbyEventService
    {
        private readonly DraftPuckContext _dbContext;
        private readonly IHubContext<LobbyHub> _hubContext;

        public LobbyEventService(DraftPuckContext dbContext, IHubContext<LobbyHub> hubContext)
        {
            _dbContext = dbContext;
            _hubContext = hubContext;
        }

        public async Task SendLobbyCreatedEvent(Lobby lobby, LobbyMember lobbyMember)
            => await NewLobbyEvent(lobby.Id, lobby.JoinCode, LobbyEventType.LobbyCreated, lobbyMemberId: lobbyMember.Id);

        public async Task SendUserJoinedEvent(Lobby lobby, LobbyMember lobbyMember)
            => await NewLobbyEvent(lobby.Id, lobby.JoinCode, LobbyEventType.UserJoined, lobbyMemberId: lobbyMember.Id);

        public async Task SendUserRejoinedEvent(Lobby lobby, LobbyMember lobbyMember)
            => await NewLobbyEvent(lobby.Id, lobby.JoinCode, LobbyEventType.UserRejoined, lobbyMemberId: lobbyMember.Id);

        public async Task SendUserRemovedEvent(Lobby lobby, LobbyMember lobbyMember)
            => await NewLobbyEvent(lobby.Id, lobby.JoinCode, LobbyEventType.UserRemoved, lobbyMemberId: lobbyMember.Id, text: $"<strong>{lobbyMember.Name}</strong> was removed from the lobby.");

        public async Task SendUserNameChangedEvent(Lobby lobby, LobbyMember lobbyMember, string oldName)
            => await NewLobbyEvent(lobby.Id, lobby.JoinCode, LobbyEventType.UserNameChanged, lobbyMemberId: lobbyMember.Id, title: "Name Change", text: $"<strong>{oldName}</strong> changed name to <strong>{lobbyMember.Name}</strong>.");

        public async Task SendNewPickEvent(Lobby lobby, LobbyMember lobbyMember, long gamePk, long playerId, int teamId)
            => await NewLobbyEvent(lobby.Id, lobby.JoinCode, LobbyEventType.NewPick, lobbyMemberId: lobbyMember.Id, gamePk: gamePk, playerId: playerId, teamId: teamId);

        public async Task SendDrinkAwardedEvent(Lobby lobby, LobbyMember lobbyMember, long gamePk, int gameEventId, long playerId, int teamId)
            => await NewLobbyEvent(lobby.Id, lobby.JoinCode, LobbyEventType.DrinkAwarded, lobbyMemberId: lobbyMember.Id, gameEventId: gameEventId, gamePk: gamePk, playerId: playerId, teamId: teamId);

        public async Task SendDrinkAssignedEvent(Lobby lobby, LobbyMember sender, LobbyMember recipient, long gamePk, int gameEventId, long playerId, int teamId)
            => await NewLobbyEvent(lobby.Id, lobby.JoinCode, LobbyEventType.DrinkAssigned, lobbyMemberId: sender.Id, lobbyMember2Id: recipient.Id, gameEventId: gameEventId, gamePk: gamePk, playerId: playerId, teamId: teamId);

        public async Task SendDrinkInvalidatedEvent(Lobby lobby, LobbyMember sender, LobbyMember recipient, long gamePk, int gameEventId, long playerId)
            => await NewLobbyEvent(lobby.Id, lobby.JoinCode, LobbyEventType.DrinkInvalidated, lobbyMemberId: sender.Id, lobbyMember2Id: recipient.Id, gameEventId: gameEventId, gamePk: gamePk, playerId: playerId);

        public async Task SendDrinkRemovedEvent(Lobby lobby, LobbyMember lobbyMember)
            => await NewLobbyEvent(lobby.Id, lobby.JoinCode, LobbyEventType.DrinkRevoked, lobbyMemberId: lobbyMember.Id);

        public async Task SendGoalChangedEvent(long gamePk, long newPlayerId, long oldPlayerId, int teamId)
            => await NewGlobalEvent(LobbyEventType.GoalChanged, playerId: newPlayerId, player2Id: oldPlayerId, gamePk: gamePk, teamId: teamId);

        public async Task SendGoalRemovedEvent(long gamePk, long playerId)
            => await NewGlobalEvent(LobbyEventType.GoalRemoved, playerId: playerId, gamePk: gamePk);

        private async Task NewLobbyEvent(Guid lobbyId, string joinCode, LobbyEventType eventType, DateTime? timeUtc = null, Guid? lobbyMemberId = null, int? gameEventId = null, long? gamePk = null, Guid? lobbyMember2Id = null, long? playerId = null, long? player2Id = null, int? teamId = null, string? title = null, string? text = null)
        {
            var lobbyEvent = new LobbyEvent()
            {
                TimeUtc = timeUtc ?? DateTime.UtcNow,
                Title = title ?? GetTitle(eventType),
                Text = text ?? GetText(eventType),
                GameEventId = gameEventId,
                GamePk = gamePk,
                LobbyId = lobbyId,
                LobbyEventType = eventType,
                LobbyMemberId = lobbyMemberId,
                SendAttempts = 1,
                LastSendAttempt = DateTime.UtcNow,
                LobbyMember2Id = lobbyMember2Id,
                PlayerId = playerId,
                Player2Id= player2Id,
                TeamId = teamId
            };

            try
            {
                await _hubContext.Clients.Group(joinCode).SendAsync("LobbyEvent", lobbyEvent);
                lobbyEvent.IsSent = true;
            }
            finally
            {
                _dbContext.LobbyEvents.Add(lobbyEvent);
                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task NewGlobalEvent(LobbyEventType eventType, DateTime? timeUtc = null, int? gameEventId = null, long? gamePk = null, long? playerId = null, long? player2Id = null, int? teamId = null)
        {
            var lobbyEvent = new LobbyEvent()
            {
                TimeUtc = timeUtc ?? DateTime.UtcNow,
                Title = GetTitle(eventType),
                Text = GetText(eventType),
                GameEventId = gameEventId,
                GamePk = gamePk,
                LobbyEventType = eventType,
                SendAttempts = 1,
                LastSendAttempt = DateTime.UtcNow,
                PlayerId = playerId,
                Player2Id = player2Id,
                TeamId = teamId
            };

            try
            {
                await _hubContext.Clients.All.SendAsync("LobbyEvent", lobbyEvent);
                lobbyEvent.IsSent = true;
            }
            finally
            {
                _dbContext.LobbyEvents.Add(lobbyEvent);
                await _dbContext.SaveChangesAsync();
            }
        }

        private static string GetTitle(LobbyEventType eventType)
            => LobbyEventTexts.GetTitle(eventType);

        private static string GetText(LobbyEventType eventType)
            => LobbyEventTexts.GetText(eventType);
    }
}
