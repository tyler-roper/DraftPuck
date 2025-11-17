namespace DraftPuck.Application.Features.Achievements.Rules;

public class EightMemberLobbyRule(ILogger<EightMemberLobbyRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "party_starter";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.LobbyJoined];
    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        return await dbContext.Lobbies
                .Where(l => l.CreatedBy == user.Id)
                .AnyAsync(l => l.LobbyMembers.Count(lm => !lm.IsBot && !lm.IsRemoved) >= 8);
    }
}