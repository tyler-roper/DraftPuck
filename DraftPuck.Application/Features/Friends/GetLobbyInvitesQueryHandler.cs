namespace DraftPuck.Application.Features.Friends;

public class GetLobbyInvitesQueryHandler(IDbContext dbContext) : IRequestHandler<GetLobbyInvitesQuery, List<Guid>>
{
    public async Task<List<Guid>> Handle(GetLobbyInvitesQuery request, CancellationToken ct)
    {
        var sender = await LobbyInviteAccess.SenderAsync(dbContext, request.UserId, request.JoinCode, ct);
        return await LobbyInviteAccess.PendingRecipientsAsync(dbContext, sender.LobbyId, ct);
    }
}
