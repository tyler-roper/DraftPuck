using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DraftPuck.Infrastructure.SignalR;

[Authorize]
public class LobbyHub(IDbContext dbContext, ILogger<LobbyHub> logger) : Hub
{
    public async Task JoinLobby(string lobbyCode)
    {
        if (!Guid.TryParse(Context.UserIdentifier, out var userId))
            throw new HubException("Unauthorized.");

        var isMember = await dbContext.LobbyMembers
            .AnyAsync(lm => lm.UserId == userId && !lm.IsRemoved && lm.Lobby.JoinCode == lobbyCode, Context.ConnectionAborted);

        if (!isMember)
            throw new HubException("You are not a member of this lobby.");

        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyCode);
        logger.LogDebug("User {userId} joined lobby group {lobbyCode}.", userId, lobbyCode);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogDebug("Connection {connectionId} disconnected. (Reason: {reason})", Context.ConnectionId, exception?.Message ?? "None");
        return base.OnDisconnectedAsync(exception);
    }
}
