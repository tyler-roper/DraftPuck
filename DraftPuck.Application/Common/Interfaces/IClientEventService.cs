using DraftPuck.Application.Features.Lobbies.Events;
using DraftPuck.Application.Features.Lobbies.Messages;

namespace DraftPuck.Application.Common.Interfaces;

public interface IClientEventService
{
    public Task SendLobbyEvent(string joinCode, LobbyEventDto lobbyEvent, CancellationToken ct);
    public Task SendGlobalLobbyEvent(LobbyEventDto lobbyEvent, CancellationToken ct);
    public Task SendMessage(string joinCode, MessageDto message, CancellationToken ct);
    public Task SendLobbyStateChangedNotification(string joinCode, CancellationToken ct);

    /// <summary>Sends a game's latest state to every connected client. Every lobby page shows every game of the day.</summary>
    public Task SendGameUpdated(GameDto game, CancellationToken ct);
}