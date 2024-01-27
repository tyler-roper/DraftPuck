namespace DraftPuck.Core.Models;

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
