using DraftPuck.Application.Features.Lobbies.Picks;

namespace DraftPuck.Application.Features.Games;

/// <summary>
/// Keeps a game's rosters in step with the NHL's lineup (its player summaries) on each poll.
/// </summary>
public class GameRosterSync(IDbContext dbContext, IMediator mediator, INhlClient nhlClient, ILogger<GameRosterSync> logger)
{
    /// <summary>
    /// Removes picks of players who left the lineup, then, unless <paramref name="fetchChangedRosters"/> is false, refetches
    /// any roster whose players changed. Both games end up with the synced rosters; <paramref name="current"/>'s are sorted
    /// by goals, best first. <paramref name="previous"/> may be the same object as <paramref name="current"/> on a first poll.
    /// </summary>
    public async Task SyncAsync(GameDto previous, GameDto current, bool fetchChangedRosters, CancellationToken ct)
    {
        TeamSync[] teams =
        [
            new(previous.HomeTeam, current.HomeTeam, LineupFor(previous.HomeTeam)),
            new(previous.AwayTeam, current.AwayTeam, LineupFor(previous.AwayTeam))
        ];

        current.FormerPlayers = FormerPlayers(previous, current);
        await RemovePicksForPlayersWhoLeftAsync(current, teams, ct);

        foreach (var team in teams)
        {
            var roster = team.Previous.Roster;
            var lineupChanged = !roster.Select(p => p.Id).ToHashSet().SetEquals(team.Lineup.Select(s => s.Id));

            if (fetchChangedRosters && lineupChanged)
                roster = await FetchPlayersAsync(team.Lineup, team.Previous.Id);

            team.Previous.Roster = roster;
            team.Current.Roster = [.. roster.OrderByDescending(p => p.Goals)];
        }

        List<PlayerSummaryDto> LineupFor(GameTeamDto team) => [.. current.PlayerSummaries.Where(ps => ps.TeamId == team.Id)];
    }

    private sealed record TeamSync(GameTeamDto Previous, GameTeamDto Current, List<PlayerSummaryDto> Lineup);

    /// <summary>
    /// Everyone who was in the lineup on an earlier poll and isn't now, so the feed can still name them.
    /// A player who returns to the lineup is dropped from the list.
    /// </summary>
    private static List<PlayerSummaryDto> FormerPlayers(GameDto previous, GameDto current)
    {
        var lineupIds = current.PlayerSummaries.Select(s => s.Id).ToHashSet();
        return [.. previous.FormerPlayers.Concat(previous.PlayerSummaries)
            .Where(p => !lineupIds.Contains(p.Id))
            .DistinctBy(p => p.Id)];
    }

    private async Task<List<PlayerDto>> FetchPlayersAsync(List<PlayerSummaryDto> lineup, int teamId)
    {
        var players = new List<PlayerDto>();

        foreach (var summary in lineup)
        {
            if (await nhlClient.GetPlayerAsync(summary.Id) is not { } player) continue;

            // A player's usual team can differ from the one they play for here (e.g. LAK during USA vs Canada).
            player.TeamId = teamId;
            players.Add(player);
        }

        return players;
    }

    private async Task RemovePicksForPlayersWhoLeftAsync(GameDto game, TeamSync[] teams, CancellationToken ct)
    {
        var departedPlayerIds = teams
            .SelectMany(t =>
            {
                var lineupIds = t.Lineup.Select(s => s.Id).ToHashSet();
                return t.Previous.Roster.Where(p => !lineupIds.Contains(p.Id)).Select(p => p.Id);
            })
            .ToList();

        if (departedPlayerIds.Count == 0)
            return;

        var picksToRemove = await dbContext.LobbyMemberPicks
            .AsNoTracking()
            .Include(p => p.LobbyMember)
                .ThenInclude(lm => lm.Lobby)
            .Where(p => departedPlayerIds.Contains(p.PlayerId) && p.GameId == game.Id && p.IsActive)
            .ToListAsync(ct);

        foreach (var pick in picksToRemove)
        {
            await mediator.Send(new RemovePickCommand
            {
                PickId = pick.Id,
                Code = pick.LobbyMember.Lobby.JoinCode,
                UserId = pick.LobbyMember.UserId,
                IsSystemRemoval = true
            }, ct);
        }

        logger.LogInformation(
            "Removed {count} picks because players left the lineup for {away} @ {home}.",
            picksToRemove.Count,
            game.AwayTeam.Abbreviation,
            game.HomeTeam.Abbreviation
        );
    }
}
