namespace DraftPuck.Application.Features.Lobbies.Recap;

/// <summary>Finished lobbies' recaps, which only change while achievements from the night can still arrive.</summary>
public interface ILobbyRecapCache
{
    Task<LobbyRecapDto?> GetAsync(Guid lobbyId);
    Task SetAsync(Guid lobbyId, LobbyRecapDto recap, TimeSpan expiry);
}
