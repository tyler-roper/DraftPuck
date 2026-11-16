using System.Text.Json;
using DraftPuck.Application.Features.Lobbies.Recap;

namespace DraftPuck.Tests.Support;

/// <summary>An <see cref="ILobbyRecapCache"/> for one test: recaps round-trip through JSON, and expiries are recorded.</summary>
public class InMemoryLobbyRecapCache : ILobbyRecapCache
{
    private readonly Dictionary<Guid, string> _recaps = [];
    public Dictionary<Guid, TimeSpan> Expiries { get; } = [];

    public Task<LobbyRecapDto?> GetAsync(Guid lobbyId) =>
        Task.FromResult(_recaps.TryGetValue(lobbyId, out var json) ? JsonSerializer.Deserialize<LobbyRecapDto>(json) : null);

    public Task SetAsync(Guid lobbyId, LobbyRecapDto recap, TimeSpan expiry)
    {
        _recaps[lobbyId] = JsonSerializer.Serialize(recap);
        Expiries[lobbyId] = expiry;
        return Task.CompletedTask;
    }
}
