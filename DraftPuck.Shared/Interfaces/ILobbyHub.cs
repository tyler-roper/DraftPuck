using DraftPuck.Shared.Entities;
using DraftPuck.Shared.Models;

namespace DraftPuck.Shared.Interfaces;
public interface ILobbyHub
{
    public Task JoinLobby(string lobbyCode);
    public Task SendMessage(string lobbyCode, MessageModel message);
    public Task SendLobbyEvent(string lobbyCode, LobbyEvent lobbyEvent);
    public Task SendGlobalLobbyEvent(LobbyEvent lobbyEvent);
}
