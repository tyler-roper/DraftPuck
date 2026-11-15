namespace DraftPuck.Application.Features.Achievements.Rules;

public class FirstLobbyJoinedRule(ILogger<FirstLobbyJoinedRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "party_crasher";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyJoined];

    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        // "Join" means someone else's lobby: hosting your own doesn't count.
        return await dbContext.LobbyMembers.AnyAsync(lm => lm.UserId == user.Id && !lm.IsRemoved && lm.Lobby.CreatedBy != user.Id);
    }
}