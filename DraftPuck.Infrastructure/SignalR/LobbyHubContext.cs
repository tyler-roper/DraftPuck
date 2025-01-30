using DraftPuck.Shared.Entities;
using DraftPuck.Shared.Interfaces;
using DraftPuck.Shared.Models;
using Microsoft.AspNetCore.SignalR;

namespace DraftPuck.Infrastructure.SignalR;
public class LobbyHubContext : ILobbyHubContext
{

    private readonly IHubContext<LobbyHub> _hubContext;

    public LobbyHubContext(IHubContext<LobbyHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendMessage(string lobbyCode, MessageModel message)
    {
        await _hubContext.Clients.Group(lobbyCode).SendAsync("Message", message);
    }

    public async Task SendLobbyEvent(string lobbyCode, LobbyEvent lobbyEvent)
    {
        await _hubContext.Clients.Group(lobbyCode).SendAsync("LobbyEvent", lobbyEvent);
    }

    public async Task SendGlobalLobbyEvent(LobbyEvent lobbyEvent)
    {
        await _hubContext.Clients.All.SendAsync("LobbyEvent", lobbyEvent);
    }
}
