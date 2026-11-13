using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Games;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace DraftPuck.Application.Features.Lobbies.Picks;

public class MakePickCommandHandler(IDbContext dbContext, IMapper mapper, IMediator mediator, IGameCache gameCache, IOptions<ApplicationOptions> appConfig) : IRequestHandler<MakePickCommand, LobbyMemberPickDto>
{
    private readonly ApplicationOptions _appConfig = appConfig.Value;

    public async Task<LobbyMemberPickDto> Handle(MakePickCommand request, CancellationToken ct)
    {
        var lobby = await dbContext.Lobbies
            .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved))
            .AsSplitQuery()
            .FirstOrDefaultAsync(l => l.JoinCode == request.Code, ct)
            ?? throw new NotFoundException("Lobby not found.");

        var requester = lobby.LobbyMembers.SingleOrDefault(lm => lm.UserId == request.UserId)
            ?? throw new ForbiddenException("You are not a member of this lobby.");

        var lobbyMember = GetPickingMember(lobby, requester, request.LobbyMemberId);

        await ValidatePickAsync(lobby, lobbyMember, request, ct);

        var pick = new LobbyMemberPickEntity
        {
            LobbyMemberId = lobbyMember.Id,
            PlayerId = request.PlayerId,
            GameId = request.GameId,
            TeamId = request.TeamId
        };

        dbContext.LobbyMemberPicks.Add(pick);
        await dbContext.SaveChangesAsync(ct);

        await EnsurePickWonRaceAsync(lobby, pick, ct);

        await mediator.Publish(new PickMadeNotification(new(lobby, lobbyMember, pick)), ct);
        return mapper.Map<LobbyMemberPickDto>(pick);
    }

    /// <summary>
    /// Members pick for themselves. Only the host may pick on behalf of someone else, and only for a bot.
    /// </summary>
    private static LobbyMemberEntity GetPickingMember(LobbyEntity lobby, LobbyMemberEntity requester, Guid? lobbyMemberId)
    {
        if (lobbyMemberId == null || lobbyMemberId == requester.Id)
            return requester;

        var target = lobby.LobbyMembers.FirstOrDefault(lm => lm.Id == lobbyMemberId)
            ?? throw new NotFoundException("Lobby member not found.");

        if (lobby.CreatedBy != requester.UserId || !target.IsBot)
            throw new ForbiddenException("You can only make picks for yourself.");

        return target;
    }

    private async Task ValidatePickAsync(LobbyEntity lobby, LobbyMemberEntity member, MakePickCommand request, CancellationToken ct)
    {
        if (!lobby.GameIds.Contains(request.GameId))
            throw new BadRequestException("This game is not part of the lobby.");

        var game = await gameCache.GetGameByIdAsync(request.GameId)
            ?? throw new BadRequestException("This game is not available for picking.");

        if (request.TeamId != game.HomeTeam.Id && request.TeamId != game.AwayTeam.Id)
            throw new BadRequestException("This team is not playing in this game.");

        var isPlayerOnTeam = game.PlayerSummaries.Any(ps => ps.Id == request.PlayerId && ps.TeamId == request.TeamId)
            || (request.TeamId == game.HomeTeam.Id ? game.HomeTeam : game.AwayTeam).Roster.Any(p => p.Id == request.PlayerId);
        if (!isPlayerOnTeam)
            throw new BadRequestException("This player is not on that team's roster.");

        if (game.IsPickingClosed)
            throw new BadRequestException("Picking has closed for this game.");

        if (!game.IsPickingOpen(_appConfig.CurrentTimeUtc))
            throw new BadRequestException("Picking hasn't opened for this game yet.");

        var memberPicksForTeam = await dbContext.LobbyMemberPicks
            .CountAsync(p => p.IsActive
                && p.LobbyMemberId == member.Id
                && p.GameId == request.GameId
                && p.TeamId == request.TeamId, ct);
        if (memberPicksForTeam >= lobby.PicksPerTeam)
            throw new BadRequestException("No picks remaining for this team.");

        var lobbyMemberIds = lobby.LobbyMembers.Select(lm => lm.Id).ToList();
        var playerAlreadyPicked = await dbContext.LobbyMemberPicks
            .AnyAsync(p => p.IsActive
                && lobbyMemberIds.Contains(p.LobbyMemberId)
                && p.PlayerId == request.PlayerId
                && p.GameId == request.GameId, ct);
        if (playerAlreadyPicked)
            throw new ValidationException("Player already picked.");
    }

    /// <summary>
    /// The checks above can race with a concurrent pick (same player, or the same member's last pick for a team).
    /// After saving, re-read the competing picks in a deterministic order; if this pick lost, undo it.
    /// </summary>
    private async Task EnsurePickWonRaceAsync(LobbyEntity lobby, LobbyMemberPickEntity pick, CancellationToken ct)
    {
        var lobbyMemberIds = lobby.LobbyMembers.Select(lm => lm.Id).ToList();

        var samePlayerPicks = await dbContext.LobbyMemberPicks
            .Where(p => p.IsActive
                && lobbyMemberIds.Contains(p.LobbyMemberId)
                && p.GameId == pick.GameId
                && p.PlayerId == pick.PlayerId)
            .OrderBy(p => p.Created).ThenBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var sameTeamPicks = await dbContext.LobbyMemberPicks
            .Where(p => p.IsActive
                && p.LobbyMemberId == pick.LobbyMemberId
                && p.GameId == pick.GameId
                && p.TeamId == pick.TeamId)
            .OrderBy(p => p.Created).ThenBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var lostPlayerRace = samePlayerPicks.FirstOrDefault() != pick.Id;
        var exceededPickLimit = sameTeamPicks.IndexOf(pick.Id) >= lobby.PicksPerTeam;

        if (!lostPlayerRace && !exceededPickLimit)
            return;

        dbContext.LobbyMemberPicks.Remove(pick);
        await dbContext.SaveChangesAsync(ct);

        if (lostPlayerRace)
            throw new ValidationException("Player already picked.");

        throw new BadRequestException("No picks remaining for this team.");
    }
}
