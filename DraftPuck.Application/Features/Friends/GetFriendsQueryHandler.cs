namespace DraftPuck.Application.Features.Friends;

public class GetFriendsQueryHandler(IDbContext dbContext) : IRequestHandler<GetFriendsQuery, FriendsDto>
{
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

        var peopleIds = rows.Select(r => r.OtherId).Concat(invites.Select(i => i.SenderId)).Distinct().ToList();
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
            ]
        };
    }
}
