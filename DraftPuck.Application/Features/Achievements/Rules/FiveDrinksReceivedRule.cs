namespace DraftPuck.Application.Features.Achievements.Rules;

public class FiveDrinksReceivedRule(ILogger<FiveDrinksReceivedRule> logger) : BaseAchievementRule(logger)
{
    public override string UniqueIdentifier => "five_hole";
    public override IReadOnlyCollection<AchievementTriggerType> Triggers => [AchievementTriggerType.DrinkAssigned];
    protected override async Task<bool> IsCriteriaMetAsync(IDbContext dbContext, UserEntity user)
    {
        return await dbContext.Drinks
                .Where(d => d.RecipientLobbyMember != null && d.RecipientLobbyMember.UserId == user.Id)
                .GroupBy(d => d.RecipientLobbyMember!.LobbyId)
                .Select(g => g.Count())
                .AnyAsync(count => count >= 5);
    }
}