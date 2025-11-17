using DraftPuck.Application.Features.Lobbies;

namespace DraftPuck.Application.Common.Interfaces;

public interface IClientEventService
{
    public Task SendLobbyEvent(string joinCode, LobbyEventDto lobbyEvent, CancellationToken ct);
    public Task SendGlobalLobbyEvent(LobbyEventDto lobbyEvent, CancellationToken ct);
    public Task SendMessage(string joinCode, MessageDto message, CancellationToken ct);
    public Task SendLobbyStateChangedNotification(string joinCode, CancellationToken ct);
}