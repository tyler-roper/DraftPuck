namespace DraftPuck.Application.Features.Friends;

public class DismissLobbyInviteCommandHandler(IDbContext dbContext) : IRequestHandler<DismissLobbyInviteCommand>
{
    public async Task Handle(DismissLobbyInviteCommand request, CancellationToken ct)
    {
        var invite = await dbContext.LobbyInvites
            .FirstOrDefaultAsync(i => i.RecipientId == request.UserId && i.Lobby.JoinCode == request.JoinCode, ct);
        if (invite == null) return;

        dbContext.LobbyInvites.Remove(invite);
        await dbContext.SaveChangesAsync(ct);
    }
}
