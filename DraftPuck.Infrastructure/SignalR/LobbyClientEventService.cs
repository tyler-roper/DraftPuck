using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies.Events;
using DraftPuck.Application.Features.Lobbies.Messages;
using Microsoft.AspNetCore.SignalR;

namespace DraftPuck.Infrastructure.SignalR;

public class LobbyClientEventService(IHubContext<LobbyHub> hubContext) : IClientEventService
{
    public async Task SendLobbyEvent(string joinCode, LobbyEventDto lobbyEvent, CancellationToken ct)
        => await hubContext.Clients.Group(joinCode).SendAsync("LobbyEvent", lobbyEvent, ct);

    public async Task SendMessage(string joinCode, MessageDto message, CancellationToken ct)
        => await hubContext.Clients.Group(joinCode).SendAsync("Message", message, ct);

    public async Task SendLobbyStateChangedNotification(string joinCode, CancellationToken ct)
        => await hubContext.Clients.Group(joinCode).SendAsync("LobbyStateChanged", ct);

    // A new message name: pages still running code from before it would merge partial games into whole ones.
    public async Task SendGameChanges(GameChangesDto changes, CancellationToken ct)
        => await hubContext.Clients.All.SendAsync("GameChanged", changes, ct);
}
