using DraftPuck.Application.Common.Exceptions;

namespace DraftPuck.Application.Features.Friends;

/// <summary>Who may invite to a lobby, and who has been invited to it.</summary>
public static class LobbyInviteAccess
{
    /// <summary>
    /// The caller's membership of the lobby, with the lobby loaded. Only a signed-in member of an active lobby can
    /// invite to it (or see who's been invited).
    /// </summary>
    public static async Task<LobbyMemberEntity> SenderAsync(IDbContext dbContext, Guid userId, string joinCode, CancellationToken ct)
    {
        var member = await dbContext.LobbyMembers
            .Include(m => m.Lobby)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsRemoved && m.Lobby.JoinCode == joinCode && m.Lobby.IsActive, ct)
            ?? throw new NotFoundException("Lobby not found.");

        if (!FriendRules.CanHaveFriends(member.User))
            throw new ForbiddenException("Sign up to invite friends.");

        return member;
    }

    /// <summary>Everyone invited to the lobby who hasn't joined it or dismissed the invite.</summary>
    public static Task<List<Guid>> PendingRecipientsAsync(IDbContext dbContext, Guid lobbyId, CancellationToken ct) =>
        dbContext.LobbyInvites
            .Where(i => i.LobbyId == lobbyId)
            .Where(i => !dbContext.LobbyMembers.Any(m => m.LobbyId == lobbyId && m.UserId == i.RecipientId))
            .Select(i => i.RecipientId)
            .ToListAsync(ct);
}
