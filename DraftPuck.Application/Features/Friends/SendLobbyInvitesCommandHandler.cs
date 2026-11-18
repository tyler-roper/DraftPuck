using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Domain.Friends;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Friends;

public class SendLobbyInvitesCommandHandler(IDbContext dbContext, IMediator mediator) : IRequestHandler<SendLobbyInvitesCommand, List<Guid>>
{
    /// <summary>More than anyone's friends list needs at once; keeps one request from fanning out without limit.</summary>
    public const int MaxInvitesPerRequest = 50;

    public async Task<List<Guid>> Handle(SendLobbyInvitesCommand request, CancellationToken ct)
    {
        var requested = request.FriendIds.Distinct().ToList();
        if (requested.Count == 0)
            throw new ValidationException("Choose at least one friend to invite.");
        if (requested.Count > MaxInvitesPerRequest)
            throw new ValidationException($"You can invite at most {MaxInvitesPerRequest} friends at once.");

        var sender = await LobbyInviteAccess.SenderAsync(dbContext, request.UserId, request.JoinCode, ct);
        var lobby = sender.Lobby;

        var friendIds = dbContext.Friendships.FriendIdsOf(request.UserId);
        var invitable = await dbContext.Users
            .Where(u => requested.Contains(u.Id) && u.IsActive && friendIds.Contains(u.Id))
            .Where(u => !dbContext.LobbyMembers.Any(m => m.LobbyId == lobby.Id && m.UserId == u.Id))
            .Where(u => !dbContext.LobbyInvites.Any(i => i.LobbyId == lobby.Id && i.RecipientId == u.Id))
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (invitable.Count > 0)
        {
            var now = DateTime.UtcNow;
            dbContext.LobbyInvites.AddRange(invitable.Select(id => new LobbyInviteEntity
            {
                Id = Guid.NewGuid(),
                LobbyId = lobby.Id,
                SenderId = request.UserId,
                RecipientId = id,
                Created = now
            }));
            await dbContext.SaveChangesAsync(ct);
            await mediator.Publish(new LobbyInvitesSentNotification(lobby, sender, invitable), ct);
        }

        return await LobbyInviteAccess.PendingRecipientsAsync(dbContext, lobby.Id, ct);
    }
}
