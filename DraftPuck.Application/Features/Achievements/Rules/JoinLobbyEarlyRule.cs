using DraftPuck.Application.Features.Games;

namespace DraftPuck.Application.Features.Achievements.Rules;

public class JoinLobbyEarlyRule(ILogger<JoinLobbyEarlyRule> logger, IGameCache gameCache) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "early_riser";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyJoined, AchievementTriggerType.LobbyCreated];
    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var earliestGameTime = (await gameCache.GetAllGamesAsync())
                .Where(g => g.GameState == GameState.Upcoming)
                .OrderBy(g => g.DateTime)
                .Select(g => g.DateTime)
                .FirstOrDefault();

        if (earliestGameTime == DateTime.MinValue) return false;
        var threeHoursBeforeGame = earliestGameTime.AddHours(-3);

        var earliestLobbyJoinTime = await dbContext.LobbyMembers
                .Where(lm =>
                    lm.UserId == user.Id &&
                    !lm.IsRemoved &&
                    lm.Lobby.IsActive)
                .OrderBy(lm => lm.Joined)
                .Select(lm => lm.Joined)
                .FirstOrDefaultAsync();

        if (earliestLobbyJoinTime == DateTime.MinValue) return false;

        return earliestLobbyJoinTime <= threeHoursBeforeGame;
    }
}