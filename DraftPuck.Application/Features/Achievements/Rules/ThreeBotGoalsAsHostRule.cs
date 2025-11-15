namespace DraftPuck.Application.Features.Achievements.Rules;

public class ThreeBotGoalsAsHost(ILogger<ThreeBotGoalsAsHost> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "puppet_master";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];
    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        return await dbContext.Drinks
                .Where(d =>
                    d.LobbyMemberPick.LobbyMember.IsBot &&
                    d.RecipientLobbyMemberId != null &&
                    d.LobbyMemberPick.LobbyMember.Lobby.CreatedBy == user.Id)
                .GroupBy(d => d.LobbyMemberPick.LobbyMember.LobbyId)
                .Select(g => g.Count())
                .AnyAsync(count => count >= 3);
    }
}