using Microsoft.AspNetCore.SignalR;

namespace DraftPuck.Infrastructure.SignalR;

public class LobbyHub : Hub
{
    public async Task JoinLobby(string lobbyCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyCode);
    }
}
