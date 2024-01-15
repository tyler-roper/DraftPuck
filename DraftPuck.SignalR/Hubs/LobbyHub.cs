using Microsoft.AspNetCore.SignalR;

namespace DraftPuck.SignalR.Hubs;

public class LobbyHub : Hub
{
    public async Task JoinLobby(string lobbyCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyCode);
    }
}
