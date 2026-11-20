using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies.Events;
using DraftPuck.Application.Features.Lobbies.Messages;

namespace DraftPuck.Application.Common.Interfaces;

public interface IClientEventService
{
    public Task SendLobbyEvent(string joinCode, LobbyEventDto lobbyEvent, CancellationToken ct);
    public Task SendMessage(string joinCode, MessageDto message, CancellationToken ct);
    public Task SendLobbyStateChangedNotification(string joinCode, CancellationToken ct);

    /// <summary>
    /// Sends what a poll changed in a game to every connected client. Every lobby page shows every game of the day, so
    /// every client needs every game; sending only the changes keeps that small.
    /// </summary>
    public Task SendGameChanges(GameChangesDto changes, CancellationToken ct);
}
