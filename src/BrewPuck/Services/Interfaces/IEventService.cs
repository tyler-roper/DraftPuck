namespace BrewPuck.Services.Interfaces
{
    public interface IEventService
    {
        event EventHandler<LobbyEventArgs>? LobbyEvent;
        event EventHandler? KeepAlive;
        void Notify(LobbyEventModel lobbyEvent);
        void SendKeepAliveMessages();
    }
}
