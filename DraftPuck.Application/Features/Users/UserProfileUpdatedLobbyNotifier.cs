namespace DraftPuck.Application.Features.Users;

public class UserProfileUpdatedLobbyNotifier(IDbContext dbContext, IMediator mediator) : INotificationHandler<UserProfileUpdatedNotification>
{
    public async Task Handle(UserProfileUpdatedNotification n, CancellationToken ct)
    {
        var userLobbies = await dbContext.LobbyMembers
            .Where(lm => lm.UserId == n.Data.UserId)
            .Where(lm => lm.Lobby.IsActive == true)
            .Select(lm => lm.Lobby)
            .ToListAsync(ct);

        foreach (var lobby in userLobbies)
        {
            var notification = new LobbyStateChangedNotification(new LobbyStateChangedPayload(lobby.JoinCode));
            await mediator.Publish(notification, ct);

            if (n.Data.OldName != null)
            {
                var lobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == n.Data.UserId);
                if (lobbyMember == null) return;

                var payload = new LobbyNameChangeEventPayload(lobby, lobbyMember, n.Data.OldName);
                await mediator.Publish(new UserNameChangedNotification(payload), ct);
            }
        }
    }
}
