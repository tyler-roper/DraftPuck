using DraftPuck.Application.Features.Games;

namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>
/// "Choose a player from the bottom third of their team in goals, and have them score." The team is the skaters on
/// the roster of the game the pick was for; a player is in the bottom third when they have no more goals than the
/// skater a third of the way up, so everyone tied at that total counts.
/// </summary>
public class BottomThirdScorerRule(ILogger<BottomThirdScorerRule> logger, IGameCache gameCache) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "the_gambler";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAwarded];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var scoringPicks = await dbContext.LobbyMemberPicks
            .Where(p => p.LobbyMember.UserId == user.Id && p.Drinks.Any())
            .Select(p => new { p.GameId, p.TeamId, p.PlayerId })
            .Distinct()
            .ToListAsync();

        foreach (var pick in scoringPicks)
        {
            // Only games still in the cache: their rosters show the goal totals from when the pick was made.
            var game = await gameCache.GetGameByIdAsync(pick.GameId);
            if (game == null) continue;

            var team = game.HomeTeam.Id == pick.TeamId ? game.HomeTeam : game.AwayTeam.Id == pick.TeamId ? game.AwayTeam : null;
            if (team == null) continue;

            var skaters = team.Roster.Where(p => p.Position != "G").ToList();
            var player = skaters.FirstOrDefault(p => p.Id == pick.PlayerId);
            if (player == null || skaters.Count < 3) continue;

            var goalsAscending = skaters.Select(p => p.Goals).Order().ToList();
            var bottomThirdCutoff = goalsAscending[(int)Math.Ceiling(skaters.Count / 3.0) - 1];

            if (player.Goals <= bottomThirdCutoff)
                return true;
        }

        return false;
    }
}
