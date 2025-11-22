namespace DraftPuck.Application.Features.Achievements.Rules;

public class DiscordServerJoinedRule(ILogger<DiscordServerJoinedRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "certified_chirper";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DiscordServerJoined];

    protected override Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        // If we're here, the user has already linked Discord and joined the server
        return Task.FromResult(true);
    }
}