using DraftPuck.Shared.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace DraftPuck.Infrastructure.SignalR;

public class LobbyHub : Hub
{
    static readonly ConcurrentDictionary<string, LobbyMember> CurrentConnections = [];

    public async Task JoinLobby(string lobbyCode, LobbyMember member)
    {
        CurrentConnections.TryAdd(Context.ConnectionId, member);
        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyCode);
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        if (CurrentConnections.Remove(Context.ConnectionId, out var member))
            Console.WriteLine($"{member.Name} has disconnected. (Reason: {exception.Message})");
        return base.OnDisconnectedAsync(exception);
    }
}
