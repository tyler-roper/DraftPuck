namespace BrewPuck.Services.Interfaces
{
    public interface ILobbyEventService
    {
        void SendMessage(LobbyEventModel lobbyEvent);
    }
}