using BrewPuck.Data;
using System.Text.Json.Serialization;

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

    public interface ILobbyEventData { }

    public class LobbyEventModel
    {
        public LobbyEventModel(LobbyEventType type, Guid lobbyId, LobbyMember lobbyMember)
        {
            Type = type;
            LobbyId = lobbyId;
            EntityId = lobbyMember.Id;
        }

        public LobbyEventModel(LobbyEventType type, Guid lobbyId, LobbyMemberPick lobbyMemberPick)
        {
            Type = type;
            LobbyId = lobbyId;
            EntityId = lobbyMemberPick.Id;
        }

        public LobbyEventModel(LobbyEventType type, Guid lobbyId, Drink drink)
        {
            Type = type;
            LobbyId = lobbyId;
            EntityId = drink.Id;
        }

        public LobbyEventType Type { get; set; }
        public Guid LobbyId { get; set; }
        public Guid EntityId { get; set; }
    }
}
