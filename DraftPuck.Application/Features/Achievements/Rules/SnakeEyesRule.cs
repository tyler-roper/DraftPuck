namespace DraftPuck.Application.Features.Achievements.Rules;

/// <summary>"Pick only 2 players and have both of them score": decided when the lobby ends, from the picks you finished with.</summary>
public class SnakeEyesRule(ILogger<SnakeEyesRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "snake_eyes";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyEnded];

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
        => dbContext.LobbyMembers
            .Where(lm => lm.UserId == user.Id && !lm.IsRemoved && !lm.Lobby.IsActive)
            .AnyAsync(lm =>
                lm.LobbyMemberPicks.Count(p => p.IsActive) == 2 &&
                lm.LobbyMemberPicks.Where(p => p.IsActive).All(p => p.Drinks.Any()));
}
