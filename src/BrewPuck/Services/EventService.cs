namespace BrewPuck.Services
{
    public class EventService : IEventService
    {
        public event EventHandler<LobbyEventArgs>? LobbyEvent;
        public event EventHandler? KeepAlive;

        public void Notify(LobbyEventModel lobbyEvent)
        {
            LobbyEvent?.Invoke(this, new LobbyEventArgs(lobbyEvent));
        }

        public void SendKeepAliveMessages()
        {
            KeepAlive?.Invoke(this, EventArgs.Empty);
        }
    }

    public class LobbyEventArgs : EventArgs
    {
        public LobbyEventModel LobbyEvent { get; }

        public LobbyEventArgs(LobbyEventModel lobbyEvent)
        {
            LobbyEvent = lobbyEvent;
        }
    }

    public class LobbyEventModel
    {
        public LobbyEventType Type { get; set; }
        public Guid LobbyId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public Guid LobbyMemberId { get; set; }
    }
}
