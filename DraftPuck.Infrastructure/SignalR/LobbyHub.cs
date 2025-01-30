using DraftPuck.Shared.Entities;
using DraftPuck.Shared.Interfaces;
using DraftPuck.Shared.Models;
using Microsoft.AspNetCore.SignalR;

namespace DraftPuck.Infrastructure.SignalR;

public class LobbyHub : Hub, ILobbyHub
{

    public async Task JoinLobby(string lobbyCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyCode);
    }

    public async Task SendMessage(string lobbyCode, MessageModel message)
    {
        await Clients.Group(lobbyCode).SendAsync("Message", message);
    }

    public async Task SendLobbyEvent(string lobbyCode, LobbyEvent lobbyEvent)
    {
        await Clients.Group(lobbyCode).SendAsync("LobbyEvent", lobbyEvent);
    }

    public async Task SendGlobalLobbyEvent(LobbyEvent lobbyEvent)
    {
        await Clients.All.SendAsync("LobbyEvent", lobbyEvent);
    }
}
