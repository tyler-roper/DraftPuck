using DraftPuck.Application.Features.Lobbies;
using System.Linq.Expressions;

namespace DraftPuck.Application.Common.QueryExtensions;

public static class LobbyQueryExtensions
{
    public static IQueryable<LobbyEntity> IncludeLobbyDetails(this IQueryable<LobbyEntity> query, bool includeRemoved = false)
    {
        Expression<Func<LobbyEntity, IEnumerable<LobbyMemberEntity>>> lobbyMembersSelector =
                l => l.LobbyMembers.Where(lm => includeRemoved || !lm.IsRemoved);

        return query
            .AsSplitQuery()
            .Include(lobbyMembersSelector)
                .ThenInclude(lm => lm.LobbyMemberPicks
                    .Where(lmp => lmp.IsActive))
                    .ThenInclude(lmp => lmp.Drinks)
            .Include(lobbyMembersSelector)
                .ThenInclude(lm => lm.Messages
                    .Where(m => includeRemoved || !m.IsDeleted))
            .Include(lobbyMembersSelector)
                .ThenInclude(lm => lm.User)
                    .ThenInclude(u => u.UserBanners)
                        .ThenInclude(ub => ub.Banner)
            .Include(lobbyMembersSelector)
                .ThenInclude(lm => lm.User)
                    .ThenInclude(u => u.UserTitles)
                        .ThenInclude(ut => ut.Title);
    }

    public static IQueryable<LobbySummaryDto> AsSummaries(this IQueryable<LobbyEntity> query, Guid? userId = null)
    {

        return query.Select(l => new LobbySummaryDto()
        {
            Id = l.Id,
            IsActive = l.IsActive,
            JoinCode = l.JoinCode,
            Created = l.Created,
            CreatedBy = l.CreatedBy,
            CreatedByName = l.LobbyMembers.First(lm => lm.UserId == l.CreatedBy).Name,
            PicksPerTeam = l.PicksPerTeam,
            IsBotAutoPickingEnabled = l.IsBotAutoPickingEnabled,
            GameCount = l.GameIds.Count,
            MemberCount = l.LobbyMembers.Count,
            BotCount = l.LobbyMembers.Where(lm => lm.IsBot).Count(),
            GuestCount = l.LobbyMembers.Where(lm => !lm.IsBot && lm.User.IsGuest).Count(),
            DrinksGiven = l.LobbyMembers
                                   .Where(lm => userId == null || lm.UserId == userId)
                                   .SelectMany(lm => lm.LobbyMemberPicks)
                                   .SelectMany(lmp => lmp.Drinks)
                                   .Count(d => d.RecipientLobbyMemberId != null),
            DrinksTaken = l.LobbyMembers
                                   .SelectMany(lm => lm.LobbyMemberPicks)
                                   .SelectMany(lmp => lmp.Drinks)
                                   .Count(d => d.RecipientLobbyMember != null && (userId == null || d.RecipientLobbyMember.UserId == userId)),
            DrinksPending = l.LobbyMembers
                                   .Where(lm => userId == null || lm.UserId == userId)
                                   .SelectMany(lm => lm.LobbyMemberPicks)
                                   .SelectMany(lmp => lmp.Drinks)
                                   .Count(d => d.RecipientLobbyMemberId == null)
        });
    }
}