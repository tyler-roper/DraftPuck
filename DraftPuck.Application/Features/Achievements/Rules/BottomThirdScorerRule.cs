using DraftPuck.Application.Features.Games;

namespace DraftPuck.Application.Features.Achievements.Rules;

public class BottomThirdScorerRule(ILogger<BottomThirdScorerRule> logger, IGameCache gameCache) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "the_gambler";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAwarded];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var successfulPicks = await dbContext.LobbyMemberPicks
            .Where(p => p.LobbyMember.UserId == user.Id && p.Drinks.Count > 0)
            .Select(p => new
            {
                p.PlayerId,
                p.TeamId
            })
            .Distinct()
            .ToListAsync();

        if (successfulPicks.Count == 0)
            return false;

        foreach (var pick in successfulPicks)
            if (await IsPlayerInBottomThird(gameCache, pick.PlayerId, pick.TeamId))
                return true;

        return false;
    }

    private static async Task<bool> IsPlayerInBottomThird(IGameCache gameCache, int playerId, int teamId)
    {
        var relevantGames = (await gameCache.GetAllGamesAsync())
            .Where(g => g.HomeTeam.Id == teamId || g.AwayTeam.Id == teamId)
            .ToList();

        if (relevantGames.Count == 0)
            return false;

        var allTeamPlayers = relevantGames
            .SelectMany(g => g.HomeTeam.Id == teamId ? g.HomeTeam.Roster : g.AwayTeam.Roster)
            .DistinctBy(p => p.Id)
            .OrderBy(p => p.Goals)
            .ToList();

        if (allTeamPlayers.Count < 3)
            return false;

        var thresholdIndex = (int)Math.Ceiling(allTeamPlayers.Count / 3.0);
        var targetPlayer = allTeamPlayers.FirstOrDefault(p => p.Id == playerId);

        if (targetPlayer == null)
            return false;

        var targetPlayerRank = allTeamPlayers.IndexOf(targetPlayer) + 1;
        return targetPlayerRank <= thresholdIndex;
    }
}