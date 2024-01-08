using Microsoft.AspNetCore.SignalR;

namespace DraftPuck.Api.Hubs
{
    public class LobbyHub : Hub
    {
        public async Task JoinLobby(string lobbyCode)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, lobbyCode);
        }
    }
}
