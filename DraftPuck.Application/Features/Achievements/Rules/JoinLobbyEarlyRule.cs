using DraftPuck.Application.Features.Games;

namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>"Join or create a lobby 3+ hours before the first game starts": the lobby's own first game.</summary>
public class JoinLobbyEarlyRule(ILogger<JoinLobbyEarlyRule> logger, IGameCache gameCache) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "early_riser";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyJoined, AchievementTriggerType.LobbyCreated];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        var memberships = await dbContext.LobbyMembers
            .Where(lm => lm.UserId == user.Id && !lm.IsRemoved && lm.Lobby.IsActive)
            .Select(lm => new { lm.Joined, lm.Lobby.GameIds })
            .ToListAsync();

        foreach (var membership in memberships)
        {
            var starts = new List<DateTime>();
            foreach (var gameId in membership.GameIds)
                if (await gameCache.GetGameByIdAsync(gameId) is { } game)
                    starts.Add(game.DateTime);

            if (starts.Count > 0 && membership.Joined <= starts.Min().AddHours(-3))
                return true;
        }

        return false;
    }
}
