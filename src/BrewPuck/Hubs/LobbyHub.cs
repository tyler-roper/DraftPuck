using Microsoft.AspNetCore.SignalR;

namespace BrewPuck.Hubs
{
    public class LobbyHub : Hub {
        public async Task JoinLobby(string lobbyCode)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyCode);
        }
    }
}
