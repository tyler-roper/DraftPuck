namespace DraftPuck.Application.Features.Achievements.Rules;

public class FiveBotDrinksReceivedRule(ILogger<FiveBotDrinksReceivedRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "terminated";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];
    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        return await dbContext.Drinks
                .Where(d =>
                    d.RecipientLobbyMember != null &&
                    d.RecipientLobbyMember.UserId == user.Id &&
                    d.LobbyMemberPick.LobbyMember.IsBot &&
                    d.RecipientLobbyMember.IsRemoved == false)
                .GroupBy(d => d.RecipientLobbyMember!.LobbyId)
                .Select(g => g.Count())
                .AnyAsync(count => count >= 5);
    }
}