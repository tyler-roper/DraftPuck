namespace BrewPuck.Services.Interfaces
{
    public interface ILobbyEvent
    {
        Guid LobbyId { get; }
        LobbyEventType Type { get; }
    }
}
