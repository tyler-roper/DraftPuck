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
            Data = new UserLobbyEventData(lobbyMember);
        }

        public LobbyEventModel(LobbyEventType type, Guid lobbyId, LobbyMemberPick lobbyMemberPick)
        {
            Type = type;
            LobbyId = lobbyId;
            Data = new PickLobbyEventData(lobbyMemberPick);
        }

        public LobbyEventType Type { get; set; }
        public Guid LobbyId { get; set; }

        [JsonIgnore]
        public ILobbyEventData Data { get; set; }

        [JsonPropertyName("data")]
        public object DataObject => Data;
    }

    public class UserLobbyEventData : ILobbyEventData
    {
        public UserLobbyEventData(LobbyMember lobbyMember)
        {
            LobbyMember = lobbyMember;
        }

        public LobbyMember LobbyMember { get; set; }
    }

    public class PickLobbyEventData : ILobbyEventData
    {
        public PickLobbyEventData(LobbyMemberPick lobbyMemberPick) {
            LobbyMemberPick = lobbyMemberPick;
        }

        public LobbyMemberPick LobbyMemberPick { get; set; }
    }
}
