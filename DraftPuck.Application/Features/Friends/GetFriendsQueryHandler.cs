namespace DraftPuck.Application.Features.Friends;

public class GetFriendsQueryHandler(IDbContext dbContext) : IRequestHandler<GetFriendsQuery, FriendsDto>
{
    /// <summary>Enough to find the people you play with; the rest can be added from a lobby or a friend link.</summary>
    public const int MaxSuggestions = 20;

    public async Task<FriendsDto> Handle(GetFriendsQuery request, CancellationToken ct)
    {
        var userId = request.UserId;
        var rows = await dbContext.Friendships
            .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
            .Select(f => new { OtherId = f.RequesterId == userId ? f.AddresseeId : f.RequesterId, IsAccepted = f.Accepted != null, IsMine = f.RequesterId == userId })
            .ToListAsync(ct);

        // Lobbies the user was removed from count as joined: they aren't offered back in.
        var invites = await dbContext.LobbyInvites
            .Where(i => i.RecipientId == userId && i.Lobby.IsActive)
            .Where(i => !i.Lobby.LobbyMembers.Any(m => m.UserId == userId))
            .OrderByDescending(i => i.Created)
            .Select(i => new { i.Lobby.JoinCode, i.SenderId, GameCount = i.Lobby.GameIds.Count, MemberCount = i.Lobby.LobbyMembers.Count(m => !m.IsRemoved), i.Created })
            .ToListAsync(ct);

        var suggestionIds = await SuggestionIdsAsync(userId, [.. rows.Select(r => r.OtherId)], ct);

        var peopleIds = rows.Select(r => r.OtherId).Concat(invites.Select(i => i.SenderId)).Concat(suggestionIds).Distinct().ToList();
        if (peopleIds.Count == 0) return new FriendsDto();
        var people = await dbContext.Users.Where(u => peopleIds.Contains(u.Id) && u.IsActive).AsFriends().ToDictionaryAsync(f => f.Id, ct);

        List<FriendDto> Where(Func<bool, bool, bool> include) =>
        [
            .. rows.Where(r => include(r.IsAccepted, r.IsMine) && people.ContainsKey(r.OtherId))
                .Select(r => people[r.OtherId])
                .OrderBy(f => f.Nickname, StringComparer.OrdinalIgnoreCase)
        ];

        return new FriendsDto
        {
            Friends = Where((accepted, _) => accepted),
            Incoming = Where((accepted, mine) => !accepted && !mine),
            Outgoing = Where((accepted, mine) => !accepted && mine),
            Invites =
            [
                .. invites.Where(i => people.ContainsKey(i.SenderId)).Select(i => new LobbyInviteDto
                {
                    JoinCode = i.JoinCode,
                    Sender = people[i.SenderId],
                    GameCount = i.GameCount,
                    MemberCount = i.MemberCount,
                    Created = i.Created
                })
            ],
            Suggestions = [.. suggestionIds.Where(people.ContainsKey).Select(id => people[id])]
        };
    }

    /// <summary>
    /// Players with an account who've been in a lobby with the user, most recently first, leaving out anyone already
    /// a friend or asked either way. Guests get none: they can't add friends.
    /// </summary>
    private async Task<List<Guid>> SuggestionIdsAsync(Guid userId, List<Guid> alreadyRelated, CancellationToken ct)
    {
        var canHaveFriends = await dbContext.Users.AnyAsync(u => u.Id == userId && !u.IsGuest && !u.IsBot && u.IsActive, ct);
        if (!canHaveFriends) return [];

        var myLobbyIds = dbContext.LobbyMembers.Where(m => m.UserId == userId).Select(m => m.LobbyId);
        return await dbContext.LobbyMembers
            .Where(m => myLobbyIds.Contains(m.LobbyId) && m.UserId != userId && !m.IsBot)
            .Where(m => !m.User.IsGuest && !m.User.IsBot && m.User.IsActive)
            .Where(m => !alreadyRelated.Contains(m.UserId))
            .GroupBy(m => m.UserId)
            .Select(g => new { UserId = g.Key, LastPlayed = g.Max(m => m.Lobby.Created) })
            .OrderByDescending(x => x.LastPlayed)
            .Take(MaxSuggestions)
            .Select(x => x.UserId)
            .ToListAsync(ct);
    }
}
