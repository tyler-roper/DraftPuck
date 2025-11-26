using DraftPuck.Application.Features.Lobbies.Events;
using DraftPuck.Application.Features.Lobbies.Messages;
using DraftPuck.Web.Hubs;

namespace DraftPuck.Web.Features.Lobbies;

public class LobbyClientEventService(IHubContext<LobbyHub> hubContext) : IClientEventService
{
    public async Task SendGlobalLobbyEvent(LobbyEventDto lobbyEvent, CancellationToken ct)
        => await hubContext.Clients.All.SendAsync("LobbyEvent", lobbyEvent, ct);

    public async Task SendLobbyEvent(string joinCode, LobbyEventDto lobbyEvent, CancellationToken ct)
        => await hubContext.Clients.Group(joinCode).SendAsync("LobbyEvent", lobbyEvent, ct);

    public async Task SendMessage(string joinCode, MessageDto message, CancellationToken ct)
        => await hubContext.Clients.Group(joinCode).SendAsync("Message", message, ct);

    public async Task SendLobbyStateChangedNotification(string joinCode, CancellationToken ct)
        => await hubContext.Clients.Group(joinCode).SendAsync("LobbyStateChanged", ct);
}
